using System;
using System.Linq;
using ClamAV.Net.Client.Results;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;
using RizzziGit.EnderDrive.Server.Resources;

namespace RizzziGit.EnderDrive.Server.Connections;

public sealed partial class Connection
{
  private sealed record class ScanFileRequest : BaseFileRequest
  {
    [BsonElement("fileContentId")]
    public required ObjectId FileContentId;

    [BsonElement("fileSnapshotId")]
    public required ObjectId FileSnapshotId;
  };

  private sealed record class ScanFileResponse
  {
    [BsonElement("result")]
    public required string Result;
  };

  private FileRequestHandler<ScanFileRequest, ScanFileResponse> ScanFile =>
    async (transaction, request, userAuthentication, me, myAdminAccess, result) =>
    {
      Resource<FileContent> fileContent = await Internal_EnsureFirst(
        transaction,
        Resources.Query<FileContent>(
          transaction,
          (query) =>
            query.Where(
              (fileContent) =>
                fileContent.Id == request.FileContentId && fileContent.FileId == request.FileId
            )
        )
      );

      Resource<FileSnapshot> fileSnapshot = await Internal_EnsureFirst(
        transaction,
        Resources.Query<FileSnapshot>(
          transaction,
          (query) =>
            query.Where(
              (fileSnapshot) =>
                fileSnapshot.Id == request.FileSnapshotId
                && fileSnapshot.FileId == request.FileId
                && fileSnapshot.FileContentId == request.FileContentId
            )
        )
      );

      return new()
      {
        Result = (
          await Server.VirusScanner.Scan(
            transaction,
            result.UnlockedFile,
            fileContent,
            fileSnapshot,
            false
          )
        ).ToJson(),
      };
    };
}
