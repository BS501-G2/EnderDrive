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
    private sealed record class GetFileStarsRequest
    {
        [BsonElement("fileId")]
        public required ObjectId? FileId;

        [BsonElement("userId")]
        public required ObjectId? UserId;

        [BsonElement("pagination")]
        public required PaginationOptions? Pagination;
    };

    private sealed record class GetFileStarsResponse
    {
        [BsonElement("fileStars")]
        public required string[] FileStars;
    };

    private TransactedRequestHandler<GetFileStarsRequest, GetFileStarsResponse> GetFileStars =>
        async (transaction, request) =>
        {
            UnlockedUserAuthentication userAuthentication = Internal_EnsureAuthentication();
            User me = await Internal_Me(transaction, userAuthentication);

            User? user = null;
            File? file = null;

            if (request.UserId != null)
            {
                if (
                    request.UserId != me.Id
                    && !await Resources
                        .GetAdminAccesses(transaction, userId: me.Id)
                        .ToAsyncEnumerable()
                        .AnyAsync(transaction.CancellationToken)
                )
                {
                    throw new InvalidOperationException(
                        "Insufficient permissions to get other user's starred files."
                    );
                }

                user = Internal_EnsureExists(
                    await Resources
                        .GetUsers(transaction, id: request.UserId)
                        .ToAsyncEnumerable()
                        .FirstOrDefaultAsync(transaction.CancellationToken)
                );
            }

            if (request.FileId != null)
            {
                file = await Internal_GetFile(transaction, me, request.FileId);
                FileAccessResult fileAccessResult = await Internal_UnlockFile(
                    transaction,
                    file,
                    me,
                    userAuthentication
                );
            }

            FileStar[] fileStars = await Resources
                .GetFileStars(transaction, file, user)
                .ApplyPagination(request.Pagination)
                .ToAsyncEnumerable()
                .WhereAwait(
                    async (fileStar) =>
                    {
                        File file = await Internal_GetFile(transaction, me, request.FileId);

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
                FileStars = fileStars
                    .Select((fileStar) => JToken.FromObject(fileStar).ToString())
                    .ToArray(),
            };
        };
}
