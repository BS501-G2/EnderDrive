using System;
using ClamAV.Net.Client.Results;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;
using RizzziGit.EnderDrive.Server.Resources;

namespace RizzziGit.EnderDrive.Server.Connections;

public sealed partial class Connection
{
  private sealed record class ScanFileRequest
    : BaseFileRequest
  {
    [BsonElement(
      "fileContentId"
    )]
    public required ObjectId FileContentId;

    [BsonElement(
      "fileSnapshotId"
    )]
    public required ObjectId FileSnapshotId;
  };

  private sealed record class ScanFileResponse
  {
    [BsonElement(
      "result"
    )]
    public required string Result;
  };

  private FileRequestHandler<
    ScanFileRequest,
    ScanFileResponse
  > ScanFile =>
    async (
      transaction,
      request,
      userAuthentication,
      me,
      myAdminAccess,
      file,
      result
    ) =>
    {
      FileContent fileContent =
        await Internal_GetFirst(
          transaction,
          Resources.GetFileContents(
            transaction,
            file,
            id: request.FileContentId
          )
        );

      FileSnapshot fileSnapshot =
        await Internal_GetFirst(
          transaction,
          Resources.GetFileSnapshots(
            transaction,
            file,
            fileContent,
            request.FileSnapshotId
          )
        );

      return new()
      {
        Result =
          JToken
            .FromObject(
              await Server.VirusScanner.Scan(
                transaction,
                result.File,
                fileContent,
                fileSnapshot,
                false
              )
            )
            .ToString(),
      };
    };
}
