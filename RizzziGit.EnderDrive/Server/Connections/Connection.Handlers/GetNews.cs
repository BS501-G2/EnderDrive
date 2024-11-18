using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

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
    async (transaction, request, userAuthentication, me, myAdminAccess) =>
    {
      Resource<News>[] news = await Resources
        .Query<News>(
          transaction,
          (query) =>
            query
              .Where(
                (item) =>
                  request.Published == true
                    ? item.PublishTime != null
                    : request.Published != false || item.PublishTime == null
              )
              .Where((item) => item.Id > request.AfterId)
              .ApplyPagination(request.Pagination)
        )
        .ToArrayAsync(transaction.CancellationToken);

      return new() { NewsEntries = [.. news.ToJson()] };
    };
}
