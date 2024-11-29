using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Whisper.net;
using Whisper.net.Ggml;

namespace RizzziGit.EnderDrive.Server.Services;

using System.Diagnostics;
using System.Linq;
using Commons.Collections;
using Commons.Services;
using Core;
using MimeDetective.Storage;
using Resources;
using Utilities;

public sealed record class AudioTranscriptionContext
{
  public required WhisperFactory Factory;
  public required WaitQueue<AudioTranscriptionRequest> WaitQueue;
}

public sealed record AudioTranscriptionRequest(
  TaskCompletionSource Source,
  UnlockedFile File,
  Resource<FileData> FileData,
  Resource<AudioTranscription> AudioTranscription
);

public sealed partial class AudioTranscriber(EnderDriveServer server, string modelPath)
  : Service<AudioTranscriptionContext>("Audio Transcription", server)
{
  public EnderDriveServer Server => server;
  public ResourceManager Resources => Server.Resources;

  protected override async Task<AudioTranscriptionContext> OnStart(
    CancellationToken startupCancellationToken,
    CancellationToken serviceCancellationToken
  )
  {
    if (!System.IO.File.Exists(modelPath))
    {
      await using Stream input = await WhisperGgmlDownloader.GetGgmlModelAsync(GgmlType.Base);
      await using FileStream output = System.IO.File.OpenWrite(modelPath);

      byte[] buffer = new byte[1024 * 8];

      void print() => Debug($"{modelPath}: ({StringExtensions.ToReadableBytes(output.Length)}");

      Info($"Downloading model {modelPath}...");
      print();
      while (true)
      {
        int bufferSize = await input.ReadAsync(buffer, startupCancellationToken);

        if (bufferSize == 0)
          break;

        await output.WriteAsync(buffer.AsMemory(0, bufferSize), startupCancellationToken);

        print();
      }

      Info($"Downloading model {modelPath} completed.");
    }

    WhisperFactory factory = WhisperFactory.FromPath(modelPath);

    return new() { Factory = factory, WaitQueue = new() };
  }

  protected override async Task OnRun(
    AudioTranscriptionContext context,
    CancellationToken serviceCancellationToken
  )
  {
    using WhisperFactory factory = context.Factory;

    await foreach (
      (
        TaskCompletionSource source,
        UnlockedFile file,
        Resource<FileData> fileData,
        Resource<AudioTranscription> audioTranscription
      ) in context.WaitQueue.WithCancellation(serviceCancellationToken)
    )
    {
      try
      {
        await Resources.Transact(
          (transaction) =>
            InternalProcess(transaction, source, file, fileData, factory, audioTranscription),
          serviceCancellationToken
        );
      }
      catch { }
    }
  }

  private async Task InternalProcess(
    ResourceTransaction transaction,
    TaskCompletionSource source,
    UnlockedFile file,
    Resource<FileData> fileData,
    WhisperFactory factory,
    Resource<AudioTranscription> audioTranscription
  )
  {
    try
    {
      using WhisperProcessor processor = factory.CreateBuilder().WithLanguage("auto").Build();

      await audioTranscription.Update(
        (data) =>
        {
          data.Status = AudioTranscriptionStatus.Running;
        },
        transaction
      );

      using Stream stream = Resources.CreateFileStream(file, fileData);

      AudioTranscriptionData[] data = [];

      await foreach (SegmentData? result in processor.ProcessAsync(stream))
      {
        if (result == null)
        {
          continue;
        }

        audioTranscription.Data.Text = data = [
          .. data,
          new()
          {
            Start = result.Start,
            End = result.End,
            Text = result.Text,
            MinProbability = result.MinProbability,
            MaxProbability = result.MaxProbability,
            Probability = result.Probability,
            Language = result.Language,
            Tokens =
            [
              .. result.Tokens.Select(
                (token) =>
                  new AudioTranscriptionToken()
                  {
                    Id = token.Id,
                    Text = token.Text,
                    Start = token.Start,
                    End = token.End,
                    Probability = token.Probability,
                    ProbabilityLog = token.ProbabilityLog,
                    TimestampId = token.TimestampId,
                    TimestampProbability = token.TimestampProbability,
                    TimestampProbabilitySum = token.TimestampProbabilitySum,
                    VoiceLen = token.VoiceLen,
                    DtwTimestamp = token.DtwTimestamp,
                  }
              ),
            ],
          },
        ];

        await audioTranscription.Save(transaction);
      }
    }
    catch (Exception exception)
    {
      Error(exception);
      source.TrySetException(exception);
    }
  }

  public async Task<Resource<AudioTranscription>> Process(
    ResourceTransaction transaction,
    UnlockedFile file,
    Resource<FileData> fileData
  )
  {
    Definition? definition = await Server.MimeDetector.Inspect(transaction, file, fileData);

    if (
      !(
        definition != null
        && definition.File.MimeType != null
        && (
          definition.File.MimeType.StartsWith("audio")
          || definition.File.MimeType.StartsWith("video")
        )
      )
    )
    {
      Console.WriteLine(definition);
      throw new InvalidOperationException("Not an audio file.");
    }

    Resource<AudioTranscription> audioTranscription = await Resources.GetAudioTranscription(
      transaction,
      file.File,
      fileData
    );

    if (audioTranscription.Data.Status == AudioTranscriptionStatus.NotRunning)
    {
      TaskCompletionSource onStart = new();

      _ = Resources.Transact(
        async (transaction) =>
        {
          TaskCompletionSource source = new();

          try
          {
            audioTranscription.Data.Status = AudioTranscriptionStatus.Pending;
            await audioTranscription.Save(transaction);

            await GetContext().WaitQueue.Enqueue(new(source, file, fileData, audioTranscription));

            onStart.SetResult();
          }
          catch (Exception exception)
          {
            onStart.SetException(exception);
          }

          try
          {
            await source.Task;

            await audioTranscription.Update(
              (data) =>
              {
                data.Status = AudioTranscriptionStatus.Done;
              },
              transaction
            );
          }
          catch (Exception exception)
          {
            Error(exception);
            await audioTranscription.Update(
              (data) =>
              {
                data.Status = AudioTranscriptionStatus.Error;
              },
              transaction
            );
          }
        },
        CancellationToken.None
      );

      await onStart.Task;
    }

    return audioTranscription;
  }
}
