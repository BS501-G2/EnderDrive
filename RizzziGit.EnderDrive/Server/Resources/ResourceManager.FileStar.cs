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
}

public sealed partial class ResourceManager
{
  public async Task<FileStar?> SetFileStar(
    ResourceTransaction transaction,
    File file,
    User user,
    bool starred
  )
  {
    FileStar? star = await QueryOld<FileStar>(
        transaction,
        (query) => query.Where((item) => item.FileId == file.Id)
      )
      .ToAsyncEnumerable()
      .FirstOrDefaultAsync(transaction.CancellationToken);

    if (starred && star == null)
    {
      star = new()
      {
        Id = ObjectId.GenerateNewId(),
        FileId = file.Id,
        UserId = user.Id,
        CreateTime = DateTimeOffset.Now,
      };

      await InsertOld(transaction, star);
    }
    else if (!starred && star != null)
    {
      await DeleteOld(transaction, star);
    }

    return star;
  }

  public IQueryable<FileStar> GetFileStars(
    ResourceTransaction transaction,
    File? file = null,
    User? user = null
  ) =>
    QueryOld<FileStar>(
      transaction,
      (query) =>
        query.Where(
          (item) =>
            (file == null || file.Id == item.FileId)
            && (user == null || user.Id == item.UserId)
        )
    );
}
