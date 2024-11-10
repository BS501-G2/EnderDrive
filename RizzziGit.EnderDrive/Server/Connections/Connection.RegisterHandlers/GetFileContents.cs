using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;

namespace RizzziGit.EnderDrive.Server.Connections;

using Resources;
using Utilities;

public sealed partial class Connection
{
    private sealed class GetFileContentsRequest
    {
        [BsonElement("fileId")]
        public required ObjectId FileId;

        [BsonElement("pagination")]
        public required PaginationOptions? Pagination;
    }

    private sealed class GetFileContentsResponse
    {
        [BsonElement("fileContents")]
        public required string[] FileContents;
    }

    private TransactedRequestHandler<
        GetFileContentsRequest,
        GetFileContentsResponse
    > GetFileContents =>
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

            FileContent[] fileContents = await Resources
                .GetFileContents(transaction, file)
                .ApplyPagination(request.Pagination)
                .ToAsyncEnumerable()
                .ToArrayAsync(transaction.CancellationToken);

            return new()
            {
                FileContents = fileContents
                    .Select((fileContent) => JToken.FromObject(fileContent).ToString())
                    .ToArray(),
            };
        };
}
