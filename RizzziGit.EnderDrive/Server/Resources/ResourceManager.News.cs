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
  public required string Title;

  public required long? PublishTime;
  public required ObjectId AuthorUserId;
  public required byte[] Image;
}

public sealed partial class ResourceManager
{
  public async Task<Resource<News>> CreateNews(
    ResourceTransaction transaction,
    string title,
    UnlockedFile file,
    Resource<FileData> fileData,
    Resource<User> newsAuthor,
    long? publishTime
  )
  {
    Resource<News> news = ToResource<News>(
      transaction,
      new()
      {
        Title = title,
        AuthorUserId = newsAuthor.Id,
        PublishTime = publishTime,
        Image = (
          await ReadFromFile(
            transaction,
            file,
            fileData,
            0,
            await GetFileSize(transaction, file, fileData)
          )
        ).ToArray(),
      }
    );

    await news.Save(transaction);
    return news;
  }
}
