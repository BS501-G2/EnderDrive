using System.Linq;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

using MongoDB.Bson;
using Resources;
using Utilities;

public sealed partial class Connection
{
    private sealed record class GetNewsRequest
    {
        [BsonElement("pagination")]
        public required PaginationOptions? Pagination;

        [BsonElement("afterId")]
        public required ObjectId? AfterId;

        [BsonElement("published")]
        public required bool? Published;
    }

    private sealed record class GetNewsResponse
    {
        [BsonElement("newsEntries")]
        public required string[] NewsEntries;
    }

    private AuthenticatedRequestHandler<GetNewsRequest, GetNewsResponse> GetNews =>
        async (transaction, request, userAuthentication, me) =>
        {
            News[] news = await Resources
                .GetNews(transaction, request.Published, me.Id)
                .ApplyPagination(request.Pagination)
                .ToAsyncEnumerable()
                .ToArrayAsync(transaction.CancellationToken);

            return new() { NewsEntries = news.ToJson() };
        };
}
