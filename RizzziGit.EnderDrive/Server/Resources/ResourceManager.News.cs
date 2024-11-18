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
  public async Task<Resource<News>> CreateNews(
    ResourceTransaction transaction,
    string title,
    Resource<File>[] images,
    Resource<User> newsAuthor,
    DateTimeOffset? publishTime
  )
  {
    Resource<News> news = ToResource<News>(
      transaction,
      new()
      {
        Title = title,
        ImageFileIds = images.Select((item) => item.Id).ToArray(),
        AuthorUserId = newsAuthor.Id,
        PublishTime = publishTime,
      }
    );

    await news.Save(transaction);
    return news;
  }
}
