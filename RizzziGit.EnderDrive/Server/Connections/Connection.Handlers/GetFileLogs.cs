using System;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

using Resources;
using Utilities;

public sealed partial class Connection
{
  private sealed record class GetFileLogsRequest
  {
    [BsonElement("fileId")]
    public required ObjectId? FileId;

    [BsonElement("fileContentId")]
    public required ObjectId? FileContentId;

    [BsonElement("fileSnapshotId")]
    public required ObjectId? FileSnapshotId;

    [BsonElement("userId")]
    public required ObjectId? UserId;

    [BsonElement("pagination")]
    public required PaginationOptions? Pagination;
  }

  private sealed record class GetFileLogsResponse
  {
    [BsonElement("fileLogs")]
    public required string[] FileLogs;
  }

  private AuthenticatedRequestHandler<GetFileLogsRequest, GetFileLogsResponse> GetFileLogs =>
    async (transaction, request, userAuthentication, me, myAdminAccess) =>
    {
      if (
        me.Id != request.UserId
        && !await Resources
          .Query<AdminAccess>(transaction, (query) => query.Where((item) => item.UserId == me.Id))
          .AnyAsync(transaction)
      )
      {
        throw new InvalidOperationException(
          "User ID other than self is not allowed when not an administrator."
        );
      }

      Resource<User>? user =
        request.UserId != null
          ? await Resources
            .Query<User>(transaction, (query) => query.Where((item) => item.Id == request.UserId))
            .FirstOrDefaultAsync(transaction)
          : null;

      Resource<File>? file =
        request.FileId != null
          ? await Internal_GetFile(transaction, me, userAuthentication, request.FileId)
          : null;

      FileAccessResult? fileAccessResult =
        file != null
          ? await Resources.FindFileAccess(
            transaction,
            file,
            me,
            userAuthentication,
            FileAccessLevel.ReadWrite
          )
          : null;

      if (
        file != null
        && fileAccessResult == null
        && !await Resources
          .Query<AdminAccess>(transaction, (query) => query.Where((item) => item.UserId == me.Id))
          .AnyAsync(transaction)
      )
      {
        throw new InvalidOperationException("File access is required when not an administrator.");
      }

      Resource<FileContent>? fileContent = null;
      if (request.FileContentId != null)
      {
        if (file == null)
        {
          throw new InvalidOperationException(
            "File ID is required when file content ID is provided."
          );
        }

        fileContent =
          await Resources
            .Query<FileContent>(
              transaction,
              (query) => query.Where((item) => item.Id == request.FileContentId)
            )
            .FirstOrDefaultAsync(transaction.CancellationToken)
          ?? throw new InvalidOperationException("File content not found.");
      }

      Resource<FileSnapshot>? fileSnapshot = null;

      if (request.FileSnapshotId != null)
      {
        if (fileContent == null)
        {
          throw new InvalidOperationException(
            "File content ID is required when file snapshot ID is provided."
          );
        }

        fileSnapshot =
          await Resources
            .Query<FileSnapshot>(
              transaction,
              (query) =>
                query.Where(
                  (item) =>
                    (file == null || item.FileId == file.Id)
                    && item.FileContentId == fileContent.Id
                    && item.Id == request.FileSnapshotId
                )
            )
            .FirstOrDefaultAsync(transaction.CancellationToken)
          ?? throw new InvalidOperationException("File snapshot not found.");
      }

      Resource<FileLog>[] fileLogs = await Resources
        .Query<FileLog>(
          transaction,
          (query) =>
            query.Where(
              (item) =>
                (file == null || item.FileId == file.Id)
                && (fileContent == null || item.FileContentId == fileContent.Id)
                && (fileSnapshot == null || item.FileSnapshotId == fileSnapshot.Id)
            )
        )
        .ToArrayAsync(transaction.CancellationToken);

      return new() { FileLogs = [.. fileLogs.ToJson()] };
    };
}
