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
    public required ObjectId? FileId;
    public required ObjectId? FileDataId;
    public required ObjectId? UserId;
    public required PaginationOptions? Pagination;
    public bool UniqueFileId;
  }

  private sealed record class GetFileLogsResponse
  {
    public required string[] FileLogs;
  }

  private AuthenticatedRequestHandler<GetFileLogsRequest, GetFileLogsResponse> GetFileLogs =>
    async (transaction, request, userAuthentication, me, myAdminAccess) =>
    {
      Resource<User>? user =
        request.UserId != null
          ? await Resources
            .Query<User>(transaction, (query) => query.Where((user) => user.Id == request.UserId))
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

      Resource<FileData>? fileData = null;

      fileData =
        request.FileDataId != null
          ? await Internal_EnsureFirst(
            transaction,
            Resources.Query<FileData>(
              transaction,
              (query) =>
                query.Where(
                  (fileData) =>
                    (file == null || fileData.FileId == file.Id)
                    && fileData.Id == request.FileDataId
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
                  && (fileData == null || item.FileDataId == fileData.Id)
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
