using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Newtonsoft.Json;

namespace RizzziGit.EnderDrive.Server.Resources;

public record class FileSnapshot : ResourceData
{
    [JsonProperty("createTime")]
    [BsonRepresentation(BsonType.DateTime)]
    public required DateTimeOffset CreateTime;

    [JsonProperty("fileId")]
    public required ObjectId FileId;

    [JsonProperty("fileContentId")]
    public required ObjectId FileContentId;

    [JsonProperty("authorUserId")]
    public required ObjectId AuthorUserId;

    [JsonProperty("baseFileSnapshotId")]
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

                CreateTime = DateTimeOffset.UtcNow
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
                .ToAsyncEnumerable()
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
                .ToAsyncEnumerable()
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

    public IQueryable<FileSnapshot> GetFileSnapshots(
        ResourceTransaction transaction,
        File file,
        FileContent fileContent,
        ObjectId? snapshotId = null
    ) =>
        Query<FileSnapshot>(
            transaction,
            (query) =>
                query.Where(
                    (item) =>
                        (file.Id == item.FileId)
                        && (fileContent.Id == item.FileContentId)
                        && (snapshotId == null || item.Id == snapshotId)
                )
        );

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
            .ToAsyncEnumerable()
            .FirstOrDefaultAsync(transaction.CancellationToken);

    public async Task DeleteSnapshot(ResourceTransaction transaction, FileSnapshot fileSnapshot)
    {
        await foreach (
            VirusReport virusReport in Query<VirusReport>(
                    transaction,
                    (query) => query.Where((item) => item.FileSnapshotId == fileSnapshot.Id)
                )
                .ToAsyncEnumerable()
        )
        {
            await Delete(transaction, virusReport);
        }
    }
}
