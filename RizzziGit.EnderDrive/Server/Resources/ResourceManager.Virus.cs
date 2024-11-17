using System;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using MongoDB.Bson;
using Newtonsoft.Json;

namespace RizzziGit.EnderDrive.Server.Resources;

public record class VirusReport
  : ResourceData
{
  [JsonProperty(
    "fileId"
  )]
  public required ObjectId FileId;

  [JsonProperty(
    "fileContentId"
  )]
  public required ObjectId FileContentId;

  [JsonProperty(
    "fileSnapshotId"
  )]
  public required ObjectId FileSnapshotId;

  [JsonProperty(
    "status"
  )]
  public required VirusReportStatus Status;

  [JsonProperty(
    "viruses"
  )]
  public required string[] Viruses;
}

public enum VirusReportStatus
{
  Failed,
  Completed,
}

public sealed partial class ResourceManager
{
  public async Task<VirusReport> SetVirusReport(
    ResourceTransaction transaction,
    File file,
    FileContent fileContent,
    FileSnapshot fileSnapshot,
    VirusReportStatus status,
    string[] viruses
  )
  {
    VirusReport report =
      new()
      {
        Id =
          ObjectId.GenerateNewId(),

        FileId =
          file.Id,
        FileContentId =
          fileContent.Id,
        FileSnapshotId =
          fileSnapshot.Id,

        Status =
          status,
        Viruses =
          viruses,
      };

    await Insert(
      transaction,
      [
        report,
      ]
    );

    return report;
  }

  public ValueTask<VirusReport?> GetVirusReport(
    ResourceTransaction transaction,
    File file,
    FileContent fileContent,
    FileSnapshot fileSnapshot
  ) =>
    Query<VirusReport>(
        transaction,
        (
          query
        ) =>
          query.Where(
            (
              item
            ) =>
              item.FileId
                == file.Id
              && item.FileContentId
                == fileContent.Id
              && item.FileSnapshotId
                == fileSnapshot.Id
          )
      )
      .OrderByDescending(
        (
          item
        ) =>
          item.Id
      )
      .ToAsyncEnumerable()
      .FirstOrDefaultAsync(
        GetCancellationToken()
      );
}
