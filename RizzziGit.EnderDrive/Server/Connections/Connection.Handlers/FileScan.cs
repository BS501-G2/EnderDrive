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
  private sealed record class FileScanRequest : BaseFileRequest
  {
    public required ObjectId FileDataId;
  };

  private sealed record class FileScanResponse
  {
    public required string Result;
  };

  private FileRequestHandler<FileScanRequest, FileScanResponse> FileScan =>
    async (transaction, request, userAuthentication, me, myAdminAccess, result) =>
    {
      Resource<FileData> fileSnapshot = await Internal_EnsureFirst(
        transaction,
        Resources.Query<FileData>(
          transaction,
          (query) =>
            query.Where(
              (fileSnapshot) =>
                fileSnapshot.Id == request.FileDataId
                && fileSnapshot.FileId == request.FileId
            )
        )
      );

      return new()
      {
        Result = (
          await Server.VirusScanner.Scan(
            transaction,
            result.UnlockedFile,
            fileSnapshot,
            false
          )
        ).ToJson(),
      };
    };
}
