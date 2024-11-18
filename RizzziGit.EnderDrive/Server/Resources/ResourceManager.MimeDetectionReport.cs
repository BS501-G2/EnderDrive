using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace RizzziGit.EnderDrive.Server.Resources;

public record class MimeDetectionReport : ResourceData
{
  public required ObjectId FileId;
  public required ObjectId FileContentId;
  public required ObjectId FileSnapshotId;

  public required string? Mime;
}

public sealed partial class ResourceManager
{
  public async ValueTask<Resource<MimeDetectionReport>> GetMimeDetectionReport(
    ResourceTransaction transaction,
    Resource<File> file,
    Resource<FileContent> fileContent,
    Resource<FileSnapshot> fileSnapshot
  )
  {
    Resource<MimeDetectionReport>? mimeDetectionReport = await Query<MimeDetectionReport>(
        transaction,
        (query) =>
          query
            .Where(
              (item) =>
                item.FileId == file.Id
                && item.FileContentId == fileContent.Id
                && item.FileSnapshotId == fileSnapshot.Id
            )
            .OrderByDescending((item) => item.Id)
      )
      .FirstOrDefaultAsync(transaction);

    if (mimeDetectionReport == null)
    {
      mimeDetectionReport = ToResource<MimeDetectionReport>(
        transaction,
        new()
        {
          FileId = file.Id,
          FileContentId = fileContent.Id,
          FileSnapshotId = fileSnapshot.Id,
          Mime = null,
        }
      );

      await mimeDetectionReport.Save(transaction);
    }

    return mimeDetectionReport;
  }
}
