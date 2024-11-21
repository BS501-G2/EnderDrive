using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Resources;

public sealed record class FileLog : ResourceData
{
  [BsonElement("type")]
  public required FileLogType Type;

  [BsonElement("actorUserId")]
  public required ObjectId ActorUserId;

  [BsonElement("fileId")]
  public required ObjectId FileId;

  [BsonElement("fileContentId")]
  public required ObjectId? FileContentId;

  [BsonElement("fileSnapshotId")]
  public required ObjectId? FileSnapshotId;
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
      }
    );

    await log.Save(transaction);
    return log;
  }
}
