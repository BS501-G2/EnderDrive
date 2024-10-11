using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace RizzziGit.EnderDrive.Server.Resources;

using MongoDB.Driver;

public record class FileSnapshot : ResourceData
{
    public required ObjectId FileId;
    public required ObjectId FileContentId;
    public required ObjectId AuthorUserId;
    public required ObjectId? BaseFileSnapshotId;
}

public record class FileSize : ResourceData
{
    public required ObjectId FileSnapshotId;

    public required long Size;
}

public sealed partial class ResourceManager
{
    public async Task<FileSnapshot> CreateFileSnapshot(
        ResourceTransaction transaction,
        UnlockedFile file,
        FileContent fileContent,
        UnlockedUserAuthentication userAuthentication,
        FileSnapshot? baseFileSnapshot
    )
    {
        FileSnapshot fileSnapshot =
            new()
            {
                Id = ObjectId.GenerateNewId(),

                FileId = file.Id,
                FileContentId = fileContent.Id,
                AuthorUserId = userAuthentication.UserId,
                BaseFileSnapshotId = baseFileSnapshot?.Id,
            };

        await Insert(transaction, [fileSnapshot]);

        return fileSnapshot;
    }

    public async Task<long> GetFileSize(
        ResourceTransaction transaction,
        FileSnapshot fileSnapshot
    ) =>
        (
            await Query<FileSize>(
                    transaction,
                    (query) => query.Where((item) => item.FileSnapshotId == fileSnapshot.Id)
                )
                .FirstOrDefaultAsync(transaction.CancellationToken)
        )?.Size ?? 0;

    public async Task<long> SetSize(
        ResourceTransaction transaction,
        FileSnapshot fileSnapshot,
        long newSize
    )
    {
        await foreach (
            FileSize entry in Query<FileSize>(
                transaction,
                (query) => query.Where((item) => item.FileSnapshotId == fileSnapshot.Id)
            )
        )
        {
            await Update(
                transaction,
                entry,
                Builders<FileSize>.Update.Set(nameof(FileSize.Size), newSize)
            );

            return newSize;
        }

        await Insert<FileSize>(
            transaction,
            [
                new()
                {
                    Id = ObjectId.GenerateNewId(),

                    FileSnapshotId = fileSnapshot.Id,
                    Size = newSize
                }
            ]
        );

        return newSize;
    }

    public ValueTask<FileSnapshot?> GetLatestFileSnapshot(
        ResourceTransaction transaction,
        File file,
        FileContent fileContent
    ) =>
        Query<FileSnapshot>(
                transaction,
                (query) =>
                    query
                        .Where(
                            (item) => item.FileId == file.Id && item.FileContentId == fileContent.Id
                        )
                        .OrderByDescending((item) => item.Id)
            )
            .FirstOrDefaultAsync(transaction.CancellationToken);

    public async Task DeleteSnapshot(ResourceTransaction transaction, FileSnapshot fileSnapshot)
    {
        await foreach (
            VirusReport virusReport in Query<VirusReport>(
                transaction,
                (query) => query.Where((item) => item.FileSnapshotId == fileSnapshot.Id)
            )
        )
        {
            await Delete(transaction, virusReport);
        }
    }
}
