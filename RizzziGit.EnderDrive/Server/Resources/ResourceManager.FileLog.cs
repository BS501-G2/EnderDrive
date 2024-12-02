using System;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace RizzziGit.EnderDrive.Server.Resources;

public sealed record class FileLog : ResourceData
{
  public required FileLogType Type;
  public required ObjectId ActorUserId;
  public required ObjectId FileId;
  public required ObjectId? FileDataId;
  public required long CreateTime;
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
    Resource<FileData>? fileData = null
  )
  {
    Resource<FileLog> log = ToResource<FileLog>(
      transaction,
      new()
      {
        Type = type,
        ActorUserId = actorUser.Id,
        FileId = file.Id,
        FileDataId = fileData?.Id,
        CreateTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
      }
    );

    await log.Save(transaction);
    return log;
  }
}
