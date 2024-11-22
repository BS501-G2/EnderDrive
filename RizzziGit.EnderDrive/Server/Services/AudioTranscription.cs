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
  Resource<FileContent> FileContent,
  Resource<FileSnapshot> FileSnapshot,
  Resource<AudioTranscription> AudioTranscription
);

public sealed partial class AudioTranscriber(Server server, string modelPath)
  : Service<AudioTranscriptionContext>("Audio Transcription", server)
{
  public Server Server => server;
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
        Resource<FileContent> fileContent,
        Resource<FileSnapshot> fileSnapshot,
        Resource<AudioTranscription> audioTranscription
      ) in context.WaitQueue.WithCancellation(serviceCancellationToken)
    )
    {
      await Resources.Transact(
        (transaction) =>
          InternalProcess(
            transaction,
            source,
            file,
            fileContent,
            fileSnapshot,
            factory,
            audioTranscription
          ),
        serviceCancellationToken
      );
    }
  }

  private async Task InternalProcess(
    ResourceTransaction transaction,
    TaskCompletionSource source,
    UnlockedFile file,
    Resource<FileContent> fileContent,
    Resource<FileSnapshot> fileSnapshot,
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

      ProcessStartInfo processStartInfo = new()
      {
        FileName = "/usr/bin/ffmpeg",
        Arguments = $"-i pipe:0 -f wav -ar 16000 pipe:1",
        UseShellExecute = false,
        RedirectStandardInput = true,
        RedirectStandardOutput = true,
      };

      using Process process =
        System.Diagnostics.Process.Start(processStartInfo)
        ?? throw new InvalidOperationException("Failed to start ffmpeg process.");

      using MemoryStream memoryStream = new();

      {
        using Stream stream2 = await Resources.CreateReadStream(
          transaction,
          file,
          fileContent,
          fileSnapshot
        );

        await using Stream a = System.IO.File.OpenWrite("/tmp/test.wav");
        await stream2.CopyToAsync(a);
      }

      using Stream stream = await Resources.CreateReadStream(
        transaction,
        file,
        fileContent,
        fileSnapshot
      );

      await Task.WhenAll(
        [
          Task.Run(() => stream.CopyToAsync(process.StandardInput.BaseStream)),
          Task.Run(() => process.StandardOutput.BaseStream.CopyToAsync(memoryStream)),
        ]
      );

      try
      {
        AudioTranscriptionData[] data = [];

        await foreach (SegmentData? result in processor.ProcessAsync(memoryStream))
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

          source.SetResult();
        }
      }
      finally
      {
        process.Kill();
      }
    }
    catch (Exception exception)
    {
      source.SetException(exception);
    }
  }

  public async Task<Resource<AudioTranscription>> Process(
    ResourceTransaction transaction,
    UnlockedFile file,
    Resource<FileContent> fileContent,
    Resource<FileSnapshot> fileSnapshot
  )
  {
    Definition? definition = await Server.MimeDetector.Inspect(
      transaction,
      file,
      fileContent,
      fileSnapshot
    );

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
      fileContent,
      fileSnapshot
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

            await GetContext()
              .WaitQueue.Enqueue(new(source, file, fileContent, fileSnapshot, audioTranscription));

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
