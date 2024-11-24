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
    [BsonElement("newsIds")]
    public required string[] NewsIds;
  }

  private AuthenticatedRequestHandler<GetNewsRequest, GetNewsResponse> GetNews =>
    async (transaction, request, userAuthentication, me, myAdminAccess) =>
    {
      Resource<News>[] news = await Resources
        .Query<News>(
          transaction,
          (query) =>
            query
              .Optional(
                request.Published == true
                  ? (query) => query.Where((item) => item.PublishTime != null)
                  : null
              )
              .Optional(
                request.Published == false
                  ? (query) => query.Where((item) => item.PublishTime == null)
                  : null
              )
              .Optional(
                request.AfterId != null
                  ? (query) => query.Where((item) => item.Id > request.AfterId)
                  : null
              )
              .ApplyPagination(request.Pagination)
        )
        .ToArrayAsync(transaction.CancellationToken);

      return new() { NewsIds = [.. news.Select((news) => news.Id.ToString())] };
    };

  private sealed record class GetNewsEntryRequest
  {
    [BsonElement("newsId")]
    public required ObjectId NewsId;
  }

  private sealed record class GetNewsEntryResponse
  {
    [BsonElement("newsEntry")]
    public required string NewsEntry;
  }

  private AuthenticatedRequestHandler<GetNewsEntryRequest, GetNewsEntryResponse> GetNewsEntry =>
    async (transaction, request, userAuthentication, me, myAdminAccess) =>
    {
      Resource<News> news = await Internal_EnsureFirst(
        transaction,
        Resources.Query<News>(
          transaction,
          (query) => query.Where((news) => news.Id == request.NewsId)
        )
      );

      return new() { NewsEntry = news.ToJson() };
    };
}
