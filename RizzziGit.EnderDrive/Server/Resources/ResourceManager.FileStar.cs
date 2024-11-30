using System;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace RizzziGit.EnderDrive.Server.Resources;

public record class FileStar : ResourceData
{
  public required ObjectId FileId;
  public required ObjectId UserId;
  public required long CreateTime;

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
        (query) => query.Where((item) => item.FileId == file.Id && item.UserId == user.Id)
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
          CreateTime = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
          Starred = false,
        }
      );

      await star.Save(transaction);
    }

    return star;
  }
}
