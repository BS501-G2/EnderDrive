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
    private sealed record class GetFileAccessesRequest
    {
        [BsonElement("targetUserId")]
        public required ObjectId? TargetUserId;

        [BsonElement("targetFileId")]
        public required ObjectId? TargetFileId;

        [BsonElement("authorUserId")]
        public required ObjectId? AuthorUserId;

        [BsonElement("level")]
        public required FileAccessLevel? Level;

        [BsonElement("pagination")]
        public required PaginationOptions? Pagination;
    };

    private sealed record class GetFileAccessesResponse
    {
        [BsonElement("fileAccesses")]
        public required string[] FileAccesses;
    };

    private TransactedRequestHandler<
        GetFileAccessesRequest,
        GetFileAccessesResponse
    > GetFileAccesses =>
        async (transaction, request) =>
        {
            UnlockedUserAuthentication userAuthentication = Internal_EnsureAuthentication();
            User me = await Internal_Me(transaction, userAuthentication);

            User? targetUser = null;
            Group? targetGroup = null;
            File? targetFile = null;
            User? authorUser = null;

            if (request.TargetFileId != null)
            {
                targetFile = await Internal_GetFile(transaction, me, request.TargetFileId);
                FileAccessResult? result = await Resources.FindFileAccess(
                    transaction,
                    targetFile,
                    me,
                    userAuthentication,
                    FileAccessLevel.Manage
                );

                if (
                    result == null
                    && !await Resources
                        .GetAdminAccesses(transaction, userId: me.Id)
                        .ToAsyncEnumerable()
                        .AnyAsync(transaction.CancellationToken)
                )
                {
                    throw new InvalidOperationException("No access to this file");
                }
            }

            if (request.TargetUserId != null)
            {
                if (
                    targetFile == null
                    && request.TargetUserId != me.Id
                    && !await Resources
                        .GetAdminAccesses(transaction, userId: me.Id)
                        .ToAsyncEnumerable()
                        .AnyAsync(transaction.CancellationToken)
                )
                {
                    throw new InvalidOperationException(
                        "Target file must be set if the target user id is not yourself."
                    );
                }

                targetUser = Internal_EnsureExists(
                    await Resources
                        .GetUsers(transaction, id: request.TargetUserId)
                        .ToAsyncEnumerable()
                        .FirstOrDefaultAsync(transaction.CancellationToken)
                );
            }

            if (request.AuthorUserId != null)
            {
                if (
                    targetUser == null
                    && !await Resources
                        .GetAdminAccesses(transaction, userId: me.Id)
                        .ToAsyncEnumerable()
                        .AnyAsync(transaction.CancellationToken)
                )
                {
                    throw new InvalidOperationException("Target user must be set if not an admin.");
                }

                authorUser = Internal_EnsureExists(
                    await Resources
                        .GetUsers(transaction, id: request.AuthorUserId)
                        .ToAsyncEnumerable()
                        .FirstOrDefaultAsync(transaction.CancellationToken)
                );
            }

            FileAccess[] fileAccesses = await Resources
                .GetFileAccesses(
                    transaction,
                    targetUser,
                    targetGroup,
                    targetFile,
                    authorUser,
                    request.Level
                )
                .ApplyPagination(request.Pagination)
                .ToAsyncEnumerable()
                .WhereAwait(
                    async (fileAccess) =>
                    {
                        File file = await Internal_GetFile(transaction, me, fileAccess.FileId);

                        FileAccessResult? fileAccessResult = await Resources.FindFileAccess(
                            transaction,
                            file,
                            me,
                            userAuthentication,
                            FileAccessLevel.Read
                        );

                        return fileAccessResult != null;
                    }
                )
                .ToArrayAsync(transaction.CancellationToken);

            return new()
            {
                FileAccesses = fileAccesses
                    .Select((fileAccess) => JToken.FromObject(fileAccess).ToString())
                    .ToArray(),
            };
        };
}
