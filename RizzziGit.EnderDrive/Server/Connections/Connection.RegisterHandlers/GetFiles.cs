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
    private sealed record class GetFilesRequest
    {
        [BsonElement("parentFolderId")]
        public required ObjectId? ParentFolderId;

        [BsonElement("fileType")]
        public required FileType? FileType;

        [BsonElement("name")]
        public required string? Name;

        [BsonElement("ownerUserId")]
        public required ObjectId? OwnerUserId;

        [BsonElement("id")]
        public required ObjectId? Id;

        [BsonElement("trashOptions")]
        public required TrashOptions? TrashOptions;

        [BsonElement("pagination")]
        public required PaginationOptions? Pagination;
    }

    private sealed record class GetFilesResponse
    {
        [BsonElement("files")]
        public required string[] Files;
    }

    private TransactedRequestHandler<GetFilesRequest, GetFilesResponse> GetFiles =>
        async (transaction, request) =>
        {
            UnlockedUserAuthentication userAuthentication = Internal_EnsureAuthentication();
            User me = await Internal_Me(transaction, userAuthentication);

            File? parentFolder = await Internal_GetFile(transaction, me, request.ParentFolderId);
            if (parentFolder != null)
            {
                FileAccessResult result =
                    await Resources.FindFileAccess(
                        transaction,
                        parentFolder,
                        me,
                        userAuthentication
                    ) ?? throw new InvalidOperationException("No access to this file");
            }

            User? ownerUser =
                request.OwnerUserId != null
                    ? await Resources
                        .GetUsers(transaction, id: request.OwnerUserId)
                        .ToAsyncEnumerable()
                        .FirstOrDefaultAsync(transaction.CancellationToken)
                    : null;

            if (
                ownerUser == null
                || (
                    ownerUser.Id != me.Id
                    && !await Resources
                        .GetAdminAccesses(transaction, userId: me.Id)
                        .ToAsyncEnumerable()
                        .AnyAsync(transaction.CancellationToken)
                )
            )
            {
                throw new InvalidOperationException(
                    "Owner user is not required is required for non-admin users"
                );
            }

            File[] files = await Resources
                .GetFiles(
                    transaction,
                    parentFolder,
                    request.FileType,
                    request.Name,
                    ownerUser,
                    request.Id,
                    request.TrashOptions ?? TrashOptions.NotIncluded
                )
                .ApplyPagination(request.Pagination)
                .ToAsyncEnumerable()
                .ToArrayAsync(transaction.CancellationToken);

            return new()
            {
                Files = files.Select((file) => JToken.FromObject(file).ToString()).ToArray(),
            };
        };
}