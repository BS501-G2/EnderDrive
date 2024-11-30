using System;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using Newtonsoft.Json;
using Whisper.net;

namespace RizzziGit.EnderDrive.Server.Resources;

public sealed record class AudioTranscription : ResourceData
{
  public required ObjectId FileId;
  public required ObjectId FileDataId;
  public required AudioTranscriptionStatus Status;
  public required AudioTranscriptionData[] Text;
}

public enum AudioTranscriptionStatus
{
  NotRunning,
  Pending,
  Running,
  Done,
  Error,
}

public record class AudioTranscriptionData()
{
  public required string Text;
  public required TimeSpan Start;
  public required TimeSpan End;
  public required float MinProbability;
  public required float MaxProbability;
  public required float Probability;
  public required string Language;
  public required AudioTranscriptionToken[] Tokens;
}

public record class AudioTranscriptionToken()
{
  public AudioTranscriptionToken(WhisperToken source)
    : this()
  {
    Id = source.Id;
    TimestampId = source.TimestampId;
    Probability = source.Probability;
    ProbabilityLog = source.ProbabilityLog;
    TimestampProbability = source.TimestampProbability;
    TimestampProbabilitySum = source.TimestampProbabilitySum;
    Start = source.Start;
    End = source.End;
    DtwTimestamp = source.DtwTimestamp;
    VoiceLen = source.VoiceLen;
    Text = source.Text;
  }

  public required int Id;
  public required int TimestampId;
  public required float Probability;
  public required float ProbabilityLog;
  public required float TimestampProbability;
  public required float TimestampProbabilitySum;
  public required long Start;
  public required long End;
  public required long DtwTimestamp;
  public required float VoiceLen;
  public required string? Text;
}

public sealed record class UnlockedAudioTranscription(Resource<AudioTranscription> Resource)
  : UnlockedResource<AudioTranscription>(Resource) { }

public sealed partial class ResourceManager
{
  public async Task<Resource<AudioTranscription>> GetAudioTranscription(
    ResourceTransaction transaction,
    Resource<File> file,
    Resource<FileData> fileSnapshot
  )
  {
    Resource<AudioTranscription>? audioTranscription = await Query<AudioTranscription>(
        transaction,
        (query) =>
          query.Where(
            (audioTranscription) =>
              (audioTranscription.FileId == file.Id)
              && (audioTranscription.FileDataId == fileSnapshot.Id)
          )
      )
      .FirstOrDefaultAsync(transaction);

    if (audioTranscription == null)
    {
      audioTranscription = ToResource<AudioTranscription>(
        transaction,
        new()
        {
          FileId = file.Id,
          FileDataId = fileSnapshot.Id,
          Text = [],
          Status = AudioTranscriptionStatus.NotRunning,
        }
      );

      await audioTranscription.Save(transaction);
    }

    return audioTranscription;
  }
}
