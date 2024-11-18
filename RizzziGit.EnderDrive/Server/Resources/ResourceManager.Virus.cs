using System;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using MongoDB.Bson;
using Newtonsoft.Json;

namespace RizzziGit.EnderDrive.Server.Resources;

public record class VirusReport : ResourceData
{
  [JsonProperty("fileId")]
  public required ObjectId FileId;

  [JsonProperty("fileContentId")]
  public required ObjectId FileContentId;

  [JsonProperty("fileSnapshotId")]
  public required ObjectId FileSnapshotId;

  [JsonProperty("status")]
  public required VirusReportStatus Status;

  [JsonProperty("viruses")]
  public required string[]? Viruses;
}

public enum VirusReportStatus
{
  Pending,
  Failed,
  Completed,
}

public sealed partial class ResourceManager
{
  public async ValueTask<Resource<VirusReport>> GetVirusReport(
    ResourceTransaction transaction,
    Resource<File> file,
    Resource<FileContent> fileContent,
    Resource<FileSnapshot> fileSnapshot
  )
  {
    Resource<VirusReport>? virusReport = await Query<VirusReport>(
        transaction,
        (query) =>
          query
            .Where(
              (virusReport) =>
                virusReport.FileId == file.Id
                && virusReport.FileContentId == fileContent.Id
                && virusReport.FileSnapshotId == fileSnapshot.Id
            )
            .OrderByDescending((item) => item.Id)
      )
      .FirstOrDefaultAsync(transaction);

    if (virusReport == null)
    {
      virusReport = ToResource<VirusReport>(
        transaction,
        new()
        {
          FileId = file.Id,
          FileContentId = fileContent.Id,
          FileSnapshotId = fileSnapshot.Id,
          Status = VirusReportStatus.Pending,
          Viruses = null,
        }
      );

      await virusReport.Save(transaction);
    }

    return virusReport;
  }
}
