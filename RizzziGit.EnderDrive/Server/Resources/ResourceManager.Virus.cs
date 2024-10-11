using System;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace RizzziGit.EnderDrive.Server.Resources;

public record class VirusReport : ResourceData
{
    public required ObjectId FileId;
    public required ObjectId FileContentId;
    public required ObjectId FileSnapshotId;

    public required string[] Viruses;
}

public sealed partial class ResourceManager
{
    public async Task<VirusReport> SetVirusReport(
        ResourceTransaction transaction,
        File file,
        FileContent fileContent,
        FileSnapshot fileSnapshot,
        string[] viruses
    )
    {
        VirusReport report =
            new()
            {
                Id = ObjectId.GenerateNewId(),

                FileId = file.Id,
                FileContentId = fileContent.Id,
                FileSnapshotId = fileSnapshot.Id,

                Viruses = viruses
            };

        await Insert(transaction, [report]);

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
                (query) =>
                    query.Where(
                        (item) =>
                            item.FileId == file.Id
                            && item.FileContentId == fileContent.Id
                            && item.FileSnapshotId == fileSnapshot.Id
                    )
            )
            .FirstOrDefaultAsync(CancellationToken);
}
