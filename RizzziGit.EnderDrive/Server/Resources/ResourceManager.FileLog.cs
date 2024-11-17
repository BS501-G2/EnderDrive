using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Resources;

public sealed record class FileLog
  : ResourceData
{
  [BsonElement(
    "type"
  )]
  public required FileLogType Type;

  [BsonElement(
    "actorUserId"
  )]
  public required ObjectId ActorUserId;

  [BsonElement(
    "fileId"
  )]
  public required ObjectId FileId;

  [BsonElement(
    "fileContentId"
  )]
  public required ObjectId? FileContentId;

  [BsonElement(
    "fileSnapshotId"
  )]
  public required ObjectId? FileSnapshotId;
}

public enum FileLogType
  : byte
{
  CreateFile,
  TrashFile,
  ModifyFile,
}

public sealed partial class ResourceManager
{
  public async Task<FileLog> CreateFileLog(
    ResourceTransaction transaction,
    File file,
    User actorUser,
    FileLogType type,
    FileContent? fileContent =
      null,
    FileSnapshot? fileSnapshot =
      null
  )
  {
    FileLog log =
      new()
      {
        Id =
          ObjectId.GenerateNewId(),
        Type =
          type,
        ActorUserId =
          actorUser.Id,
        FileId =
          file.Id,
        FileContentId =
          fileContent?.Id,
        FileSnapshotId =
          fileSnapshot?.Id,
      };

    await Insert(
      transaction,
      [
        log,
      ]
    );
    return log;
  }

  public IQueryable<FileLog> GetFileLogs(
    ResourceTransaction transaction,
    File? file =
      null,
    FileContent? fileContent =
      null,
    FileSnapshot? fileSnapshot =
      null,
    User? actorUser =
      null
  ) =>
    Query<FileLog>(
      transaction,
      (
        query
      ) =>
        query.Where(
          (
            item
          ) =>
            (
              file
                == null
              || item.FileId
                == file.Id
            )
            && (
              fileContent
                == null
              || item.FileContentId
                == fileContent.Id
            )
            && (
              fileSnapshot
                == null
              || item.FileSnapshotId
                == fileSnapshot.Id
            )
            && (
              actorUser
                == null
              || item.ActorUserId
                == actorUser.Id
            )
        )
    );
}
