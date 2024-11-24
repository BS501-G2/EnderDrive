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

    [BsonElement("uniqueFileId")]
    public bool UniqueFileId;
  }

  private sealed record class GetFileLogsResponse
  {
    [BsonElement("fileLogs")]
    public required string[] FileLogs;
  }

  private AuthenticatedRequestHandler<GetFileLogsRequest, GetFileLogsResponse> GetFileLogs =>
    async (transaction, request, userAuthentication, me, myAdminAccess) =>
    {
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
          ? await Internal_UnlockFile(
            transaction,
            file,
            me,
            userAuthentication,
            FileAccessLevel.Read
          )
          : null;

      Resource<FileContent>? fileContent =
        request.FileContentId != null
          ? await Internal_EnsureFirst(
            transaction,
            Resources.Query<FileContent>(
              transaction,
              (query) => query.Where((item) => item.Id == request.FileContentId)
            )
          )
          : null;

      Resource<FileSnapshot>? fileSnapshot = null;

      fileSnapshot =
        request.FileSnapshotId != null
          ? await Internal_EnsureFirst(
            transaction,
            Resources.Query<FileSnapshot>(
              transaction,
              (query) =>
                query.Where(
                  (item) =>
                    (file == null || item.FileId == file.Id) && item.Id == request.FileSnapshotId
                )
            )
          )
          : null;

      Resource<FileLog>[] fileLogs = await Resources
        .Query<FileLog>(
          transaction,
          (query) =>
            query
              .Where(
                (item) =>
                  (file == null || item.FileId == file.Id)
                  && (fileContent == null || item.FileContentId == fileContent.Id)
                  && (fileSnapshot == null || item.FileSnapshotId == fileSnapshot.Id)
                  && (user == null || item.ActorUserId == user.Id)
              )
              .OrderByDescending((fileLog) => fileLog.CreateTime)
              .Optional(
                request.UniqueFileId
                  ? (query) => query.GroupBy((e) => e.FileId).Select((e) => e.First())
                  : null
              )
              .ApplyPagination(request.Pagination)
        )
        .WhereAwait(
          async (fileLog) =>
          {
            return await Resources.FindFileAccess(
                transaction,
                file
                  ?? await Internal_GetFile(
                    transaction,
                    me,
                    userAuthentication,
                    fileLog.Data.FileId
                  ),
                me,
                userAuthentication,
                FileAccessLevel.Read
              ) != null;
          }
        )
        .ToArrayAsync(transaction.CancellationToken);

      return new() { FileLogs = [.. fileLogs.ToJson()] };
    };
}
