using System;
using System.IO;
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

  [JsonProperty("authorUserId")]
  public required ObjectId AuthorUserId;

  [JsonProperty("image")]
  public required byte[] Image;
}

public sealed partial class ResourceManager
{
  public async Task<Resource<News>> CreateNews(
    ResourceTransaction transaction,
    string title,
    UnlockedFile file,
    Resource<FileContent> fileContent,
    Resource<FileSnapshot> fileSnapshot,
    Resource<User> newsAuthor,
    DateTimeOffset? publishTime
  )
  {
    using MemoryStream stream = new();
    using Stream a = await CreateReadStream(transaction, file, fileContent, fileSnapshot);

    await a.CopyToAsync(stream, transaction.CancellationToken);

    Resource<News> news = ToResource<News>(
      transaction,
      new()
      {
        Title = title,
        AuthorUserId = newsAuthor.Id,
        PublishTime = publishTime,
        Image = stream.ToArray(),
      }
    );

    await news.Save(transaction);
    return news;
  }
}
