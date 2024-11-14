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
    public async Task<MimeDetectionReport> SetMimeDetectionReport(
        ResourceTransaction transaction,
        File file,
        FileContent fileContent,
        FileSnapshot fileSnapshot,
        string? mime
    )
    {
        MimeDetectionReport report =
            new()
            {
                Id = ObjectId.GenerateNewId(),
                FileId = file.Id,
                FileContentId = fileContent.Id,
                FileSnapshotId = fileSnapshot.Id,
                Mime = mime,
            };

        await Insert(transaction, [report]);

        return report;
    }

    public ValueTask<MimeDetectionReport?> GetMimeDetectionReport(
        ResourceTransaction transaction,
        File file,
        FileContent fileContent,
        FileSnapshot fileSnapshot
    ) =>
        Query<MimeDetectionReport>(transaction)
            .Where(
                (item) =>
                    item.FileId == file.Id
                    && item.FileContentId == fileContent.Id
                    && item.FileSnapshotId == fileSnapshot.Id
            )
            .OrderByDescending((item) => item.Id)
            .ToAsyncEnumerable()
            .FirstOrDefaultAsync(GetCancellationToken());
}
