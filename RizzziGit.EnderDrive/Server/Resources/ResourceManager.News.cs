using System;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace RizzziGit.EnderDrive.Server.Resources;

public record class News : ResourceData
{
  [JsonProperty("title")]
  public required string Title;

  [JsonProperty("publishTime")]
  [BsonRepresentation(BsonType.DateTime)]
  public required DateTimeOffset? PublishTime;

  [JsonProperty("imageFileIds")]
  public required ObjectId[] ImageFileIds;

  [JsonProperty("authorUserId")]
  public required ObjectId AuthorUserId;
}

public sealed partial class ResourceManager
{
  public async Task<News> CreateNews(
    ResourceTransaction transaction,
    string title,
    File[] images,
    User newsAuthor,
    DateTimeOffset? publishTime
  )
  {
    News news =
      new()
      {
        Id = ObjectId.GenerateNewId(),
        Title = title,
        ImageFileIds = images.Select((item) => item.Id).ToArray(),
        AuthorUserId = newsAuthor.Id,
        PublishTime = publishTime,
      };

    await InsertOld(transaction, news);
    return news;
  }

  public async Task DeleteNews(ResourceTransaction transaction, News news)
  {
    await DeleteOld(transaction, news);
  }

  public IQueryable<News> GetNews(
    ResourceTransaction transaction,
    bool? published = null,
    ObjectId? id = null
  ) =>
    QueryOld<News>(
      transaction,
      (query) =>
        query
          .Where(
            (item) =>
              (
                (published == null)
                || (
                  (bool)published
                    ? item.PublishTime != null
                    : item.PublishTime == null
                )
              ) && (id == null || item.Id == id)
          )
          .OrderByDescending((item) => item.Id)
    );
}
