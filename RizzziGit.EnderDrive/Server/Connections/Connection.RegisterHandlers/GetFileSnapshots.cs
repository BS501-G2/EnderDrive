using System;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;

namespace RizzziGit.EnderDrive.Server.Connections;

using Resources;
using Utilities;

public sealed partial class Connection
{
    private sealed class GetFileSnapshotsRequest
    {
        [BsonElement("fileId")]
        public required ObjectId FileId;

        [BsonElement("fileContentId")]
        public required ObjectId? FileContentId;

        [BsonElement("pagination")]
        public required PaginationOptions? Pagination;
    }

    private sealed class GetFileSnapshotsResponse
    {
        [BsonElement("fileSnapshots")]
        public required string[] FileSnapshots;
    }

    private TransactedRequestHandler<
        GetFileSnapshotsRequest,
        GetFileSnapshotsResponse
    > GetFileSnapshots =>
        async (transaction, request) =>
        {
            ConnectionContext context = GetContext();

            UnlockedUserAuthentication userAuthentication = Internal_EnsureAuthentication();
            User me = await Internal_Me(transaction, userAuthentication);

            File file = await Internal_GetFile(transaction, me, request.FileId);
            FileAccessResult fileAccessResult = await Internal_UnlockFile(
                transaction,
                file,
                me,
                userAuthentication
            );

            FileContent fileContent =
                await Resources
                    .GetFileContents(transaction, file, id: request.FileContentId)
                    .ToAsyncEnumerable()
                    .FirstOrDefaultAsync(transaction.CancellationToken)
                ?? throw new InvalidOperationException("Invalid file content id.");

            FileSnapshot[] fileSnapshots = await Resources
                .GetFileSnapshots(transaction, file, fileContent)
                .ApplyPagination(request.Pagination)
                .ToAsyncEnumerable()
                .ToArrayAsync(transaction.CancellationToken);

            return new()
            {
                FileSnapshots =
                [
                    .. fileSnapshots.Select(
                        (fileSnapshot) => JToken.FromObject(fileSnapshot).ToString()
                    ),
                ],
            };
        };
}
