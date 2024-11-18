using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Newtonsoft.Json;

namespace RizzziGit.EnderDrive.Server.Resources;

public record class FileSnapshot : ResourceData
{
  [JsonProperty("createTime")]
  [BsonRepresentation(representation: BsonType.DateTime)]
  public required DateTimeOffset CreateTime;

  [JsonProperty("fileId")]
  public required ObjectId FileId;

  [JsonProperty("fileContentId")]
  public required ObjectId FileContentId;

  [JsonProperty("authorUserId")]
  public required ObjectId AuthorUserId;

  [JsonProperty("baseFileSnapshotId")]
  public required ObjectId? BaseFileSnapshotId;

  [JsonProperty("size")]
  public required long Size;
}

public sealed partial class ResourceManager
{
  public async Task<Resource<FileSnapshot>> CreateFileSnapshot(
    ResourceTransaction transaction,
    UnlockedFile file,
    Resource<FileContent> fileContent,
    UnlockedUserAuthentication userAuthentication,
    Resource<FileSnapshot>? baseFileSnapshot
  )
  {
    Resource<FileSnapshot> fileSnapshot = ToResource<FileSnapshot>(
      transaction,
      new()
      {
        Id = ObjectId.Empty,

        FileId = file.File.Id,
        FileContentId = fileContent.Id,
        AuthorUserId = userAuthentication.UserAuthentication.Data.UserId,
        BaseFileSnapshotId = baseFileSnapshot?.Id,

        CreateTime = DateTimeOffset.UtcNow,
        Size = baseFileSnapshot?.Data.Size ?? 0,
      }
    );

    await fileSnapshot.Save(transaction);

    if (baseFileSnapshot != null)
    {
      await foreach (
        Resource<FileData> data in Query<FileData>(
          transaction,
          (query) => query.Where((item) => item.FileSnapshotId == baseFileSnapshot.Id)
        )
      )
      {
        Resource<FileData> newData = ToResource<FileData>(
          transaction,
          new()
          {
            FileId = data.Data.FileId,
            FileContentId = data.Data.FileContentId,
            FileSnapshotId = fileSnapshot.Id,
            AuthorUserId = userAuthentication.UserAuthentication.Data.UserId,
            Index = data.Data.Index,
            BufferId = data.Data.BufferId,
          }
        );

        await newData.Save(transaction);
      }
    }

    return fileSnapshot;
  }

  public async Task Delete(ResourceTransaction transaction, Resource<FileSnapshot> fileSnapshot)
  {
    await foreach (
      Resource<FileData> data in Query<FileData>(
        transaction,
        (query) => query.Where((item) => item.FileSnapshotId == fileSnapshot.Id)
      )
    )
    {
      await Delete(transaction, data);
    }

    await foreach (
      Resource<VirusReport> virusReport in Query<VirusReport>(
        transaction,
        (query) =>
          query.Where(
            (item) =>
              item.FileId == fileSnapshot.Data.FileId
              && item.FileContentId == fileSnapshot.Data.FileContentId
              && item.FileSnapshotId == fileSnapshot.Id
          )
      )
    )
    {
      await Delete(transaction, virusReport);
    }

    await foreach (
      Resource<MimeDetectionReport> mimeDetectionReport in Query<MimeDetectionReport>(
        transaction,
        (query) =>
          query.Where(
            (item) =>
              item.FileId == fileSnapshot.Data.FileId
              && item.FileContentId == fileSnapshot.Data.FileContentId
              && item.FileSnapshotId == fileSnapshot.Id
          )
      )
    )
    {
      await Delete(transaction, mimeDetectionReport);
    }
  }
}
