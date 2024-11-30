using System;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using MongoDB.Bson;
using Newtonsoft.Json;

namespace RizzziGit.EnderDrive.Server.Resources;

public record class VirusReport : ResourceData
{
  public required ObjectId FileId;
  public required ObjectId FileDataId;
  public required VirusReportStatus Status;
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
    Resource<FileData> fileSnapshot
  )
  {
    Resource<VirusReport>? virusReport = await Query<VirusReport>(
        transaction,
        (query) =>
          query
            .Where(
              (virusReport) =>
                virusReport.FileId == file.Id
                && virusReport.FileDataId == fileSnapshot.Id
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
          FileDataId = fileSnapshot.Id,
          Status = VirusReportStatus.Pending,
          Viruses = null,
        }
      );

      await virusReport.Save(transaction);
    }

    return virusReport;
  }
}
