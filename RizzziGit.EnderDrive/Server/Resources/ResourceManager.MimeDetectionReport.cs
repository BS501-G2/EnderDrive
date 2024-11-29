using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace RizzziGit.EnderDrive.Server.Resources;

public record class MimeDetectionReport : ResourceData
{
  public required ObjectId FileId;
  public required ObjectId FileDataId;

  public required string? Mime;
}

public sealed partial class ResourceManager
{
  public async ValueTask<Resource<MimeDetectionReport>> GetMimeDetectionReport(
    ResourceTransaction transaction,
    Resource<File> file,
    Resource<FileData> fileData
  )
  {
    Resource<MimeDetectionReport>? mimeDetectionReport = await Query<MimeDetectionReport>(
        transaction,
        (query) =>
          query
            .Where(
              (item) =>
                item.FileId == file.Id
                && item.FileDataId == fileData.Id
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
          FileDataId = fileData.Id,
          Mime = null,
        }
      );

      await mimeDetectionReport.Save(transaction);
    }

    return mimeDetectionReport;
  }
}
