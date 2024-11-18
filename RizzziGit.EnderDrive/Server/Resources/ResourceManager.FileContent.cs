using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;

namespace RizzziGit.EnderDrive.Server.Resources;

public record class FileContent : ResourceData
{
  [JsonProperty("fileId")]
  public required ObjectId FileId;

  [JsonProperty("main")]
  public required bool Main;

  [JsonProperty("name")]
  public required string Name;
}

public sealed partial class ResourceManager
{
  public async Task<Resource<FileContent>> GetMainFileContent(
    ResourceTransaction transaction,
    File file
  )
  {
    Resource<FileContent>? content = await Query<FileContent>(
        transaction,
        (query) => query.Where((item) => item.FileId == file.Id && item.Main)
      )
      .FirstOrDefaultAsync(transaction.CancellationToken);

    if (content == null)
    {
      content = ToResource<FileContent>(
        transaction,
        new()
        {
          Id = ObjectId.GenerateNewId(),
          FileId = file.Id,
          Main = true,
          Name = "Main Content",
        }
      );
      await content.Save(transaction);
    }

    return content;
  }

  public async Task<Resource<FileContent>> CreateFileContent(
    ResourceTransaction transaction,
    UnlockedFile file,
    string name
  )
  {
    Resource<FileContent> content = ToResource<FileContent>(
      transaction,
      new()
      {
        Id = ObjectId.GenerateNewId(),
        FileId = file.File.Id,
        Main = false,
        Name = name,
      }
    );

    await content.Save(transaction);
    return content;
  }
}
