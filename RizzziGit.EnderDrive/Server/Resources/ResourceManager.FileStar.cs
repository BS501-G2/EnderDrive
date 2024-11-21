using System;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace RizzziGit.EnderDrive.Server.Resources;

public record class FileStar : ResourceData
{
  [JsonProperty("fileId")]
  public required ObjectId FileId;

  [JsonProperty("userId")]
  public required ObjectId UserId;

  [JsonProperty("createTime")]
  [BsonRepresentation(BsonType.DateTime)]
  public required DateTimeOffset CreateTime;

  public required bool Starred;
}

public sealed partial class ResourceManager
{
  public async Task<Resource<FileStar>> GetFileStar(
    ResourceTransaction transaction,
    Resource<File> file,
    Resource<User> user
  )
  {
    Resource<FileStar>? star = await Query<FileStar>(
        transaction,
        (query) => query.Where((item) => item.FileId == file.Id)
      )
      .FirstOrDefaultAsync(transaction.CancellationToken);

    if (star == null)
    {
      star = ToResource<FileStar>(
        transaction,
        new()
        {
          FileId = file.Id,
          UserId = user.Id,
          CreateTime = DateTimeOffset.Now,
          Starred = false,
        }
      );

      await star.Save(transaction);
    }

    return star;
  }
}
