using System;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

using Resources;
using Utilities;

public sealed partial class Connection
{
  private sealed record class GetFileAccessesRequest
  {
    public required ObjectId? TargetUserId;
    public required ObjectId? TargetFileId;
    public required ObjectId? AuthorUserId;
    public required FileAccessLevel? Level;
    public required PaginationOptions? Pagination;
    public required bool? IncludePublic;
  };

  private sealed record class GetFileAccessesResponse
  {
    public required string[] FileAccesses;
  };

  private AuthenticatedRequestHandler<
    GetFileAccessesRequest,
    GetFileAccessesResponse
  > GetFileAccesses =>
    async (transaction, request, userAuthentication, me, myAdminAccess) =>
    {
      Resource<User>? targetUser =
        request.TargetUserId != null
          ? await Internal_EnsureFirst(
            transaction,
            Resources.Query<User>(
              transaction,
              (query) => query.Where((item) => item.Id == request.TargetUserId)
            )
          )
          : null;

      Resource<File>? targetFile =
        request.TargetFileId != null
          ? await Internal_GetFile(transaction, me, userAuthentication, request.TargetFileId)
          : null;

      FileAccessResult? unlockedTargetFile =
        targetFile != null
          ? await Internal_UnlockFile(
            transaction,
            targetFile,
            me,
            userAuthentication,
            FileAccessLevel.Manage
          )
          : null;

      Resource<User>? authorUser =
        request.AuthorUserId != null
          ? await Internal_EnsureFirst(
            transaction,
            Resources.Query<User>(
              transaction,
              (query) => query.Where((item) => item.Id == request.AuthorUserId)
            )
          )
          : null;

      if (myAdminAccess == null)
      {
        if (targetUser != null && targetUser.Id != me.Id)
        {
          throw new InvalidOperationException(
            "Target User ID must be set to self when not an admin."
          );
        }
        else if (
          targetFile != null
          && (
            await Internal_GetFirst(
              transaction,
              Resources.Query<FileAccess>(
                transaction,
                (query) =>
                  query.Where(
                    (item) =>
                      item.FileId == targetFile.Id
                      && item.TargetUserId == me.Id
                      && item.Level >= FileAccessLevel.Manage
                  )
              )
            ) == null
            && targetFile.Data.OwnerUserId != me.Id
          )
        )
        {
          throw new InvalidOperationException(
            "Target File must be owned by self or have manage accsess when not an admin."
          );
        }
        else if (authorUser != null && authorUser.Id != me.Id)
        {
          throw new InvalidOperationException(
            "Author User ID must be set to self when not an admin."
          );
        }
        else if (targetUser == null && targetFile == null && authorUser == null)
        {
          throw new InvalidOperationException(
            "Not enough permissions when trying to get all file accesses."
          );
        }
      }

      Resource<FileAccess>[] fileAccesses = await Resources
        .Query<FileAccess>(
          transaction,
          (query) =>
            query
              .Where(
                (fileAccess) =>
                  (request.TargetFileId == null || (fileAccess.FileId == request.TargetFileId))
                  && (
                    ((request.IncludePublic ?? false) && fileAccess.TargetUserId == null)
                    || request.TargetUserId == null
                    || (fileAccess.TargetUserId == request.TargetUserId)
                  )
                  && (
                    request.AuthorUserId == null
                    || (fileAccess.AuthorUserId == request.AuthorUserId)
                  )
                  && (request.Level == null || fileAccess.Level >= request.Level)
              )
              .OrderByDescending((fileAccess) => fileAccess.Id)
        )
        .IfAwait(
          () => !(request.IncludePublic ?? false),
          (query) =>
            query
              .GroupBy((fileAccess) => fileAccess.Data.FileId)
              .SelectAwait(
                (fileAccess) =>
                  fileAccess
                    .OrderByDescending((fileAccess) => fileAccess.Data.Level)
                    .FirstAsync(transaction.CancellationToken)
              )
        )
        .WhereAwait(
          async (fileAccess) =>
          {
            Resource<File> file = await Internal_GetFile(
              transaction,
              me,
              userAuthentication,
              fileAccess.Data.FileId
            );

            if (file.Data.TrashTime != null)
            {
              return false;
            }

            FileAccessResult? fileAccessResult = await Resources.FindFileAccess(
              transaction,
              file,
              me,
              userAuthentication,
              FileAccessLevel.Read
            );

            return fileAccessResult != null;
          }
        )
        .ApplyPagination(request.Pagination)
        .ToArrayAsync(transaction.CancellationToken);

      return new() { FileAccesses = [.. fileAccesses.ToJson()] };
    };
}
