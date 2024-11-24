using System;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace RizzziGit.EnderDrive.Server.Resources;

public sealed record class FileLog : ResourceData
{
  [JsonProperty("type")]
  public required FileLogType Type;

  [JsonProperty("actorUserId")]
  public required ObjectId ActorUserId;

  [JsonProperty("fileId")]
  public required ObjectId FileId;

  [JsonProperty("fileContentId")]
  public required ObjectId? FileContentId;

  [JsonProperty("fileSnapshotId")]
  public required ObjectId? FileSnapshotId;

  [JsonProperty("createTime")]
  public required DateTimeOffset CreateTime;
}

public enum FileLogType : byte
{
  Create,
  Update,
  Trash,
  Untrash,
  Share,
  Unshare,
  Delete,
  Read,
}

public sealed partial class ResourceManager
{
  public async Task<Resource<FileLog>> CreateFileLog(
    ResourceTransaction transaction,
    Resource<File> file,
    Resource<User> actorUser,
    FileLogType type,
    Resource<FileContent>? fileContent = null,
    Resource<FileSnapshot>? fileSnapshot = null
  )
  {
    Resource<FileLog> log = ToResource<FileLog>(
      transaction,
      new()
      {
        Type = type,
        ActorUserId = actorUser.Id,
        FileId = file.Id,
        FileContentId = fileContent?.Id,
        FileSnapshotId = fileSnapshot?.Id,
        CreateTime = DateTimeOffset.UtcNow,
      }
    );

    await log.Save(transaction);
    return log;
  }
}
