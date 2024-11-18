using System;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;

namespace RizzziGit.EnderDrive.Server.Connections;

using Resources;
using Utilities;

public sealed partial class Connection
{
  private sealed record class GetFileAccessesRequest
  {
    [BsonElement("targetUserId")]
    public required ObjectId? TargetUserId;

    [BsonElement("targetFileId")]
    public required ObjectId? TargetFileId;

    [BsonElement("authorUserId")]
    public required ObjectId? AuthorUserId;

    [BsonElement("level")]
    public required FileAccessLevel? Level;

    [BsonElement("pagination")]
    public required PaginationOptions? Pagination;
  };

  private sealed record class GetFileAccessesResponse
  {
    [BsonElement("fileAccesses")]
    public required string[] FileAccesses;
  };

  private AuthenticatedRequestHandler<
    GetFileAccessesRequest,
    GetFileAccessesResponse
  > GetFileAccesses =>
    async (transaction, request, userAuthentication, me, myAdminAccess) =>
    {
      Resource<User>? targetUser;
      if (request.TargetUserId != me.Id && myAdminAccess == null)
      {
        throw new InvalidOperationException("Target user must be set to self when not an admin.");
      }

      targetUser =
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

      Resource<User>? authorUser = null;

      if (request.TargetUserId != null)
      {
        targetUser = Internal_EnsureExists(
          await Resources
            .Query<User>(
              transaction,
              (query) => query.Where((item) => item.Id == request.TargetUserId)
            )
            .FirstOrDefaultAsync(transaction.CancellationToken)
        );
      }

      if (request.AuthorUserId != null)
      {
        if (
          targetUser == null
          && !await Resources
            .Query<AdminAccess>(
              transaction,
              (query) => query.Where((item) => item.UserId == request.AuthorUserId)
            )
            .AnyAsync(transaction.CancellationToken)
        )
        {
          throw new InvalidOperationException("Target user must be set if not an admin.");
        }

        authorUser = Internal_EnsureExists(
          await Resources
            .Query<User>(
              transaction,
              (query) => query.Where((item) => item.Id == request.AuthorUserId)
            )
            .FirstOrDefaultAsync(transaction.CancellationToken)
        );
      }

      Resource<FileAccess>[] fileAccesses = await Resources
        .Query<FileAccess>(
          transaction,
          (query) =>
            query
              .Where(
                (item) =>
                  (request.TargetFileId == null || item.FileId == request.TargetFileId)
                  && (
                    request.TargetUserId == null
                    || (
                      item.TargetEntity != null
                      && item.TargetEntity.EntityType == FileAccessTargetEntityType.User
                      && item.TargetEntity.EntityId == request.TargetUserId
                    )
                  )
                  && (request.AuthorUserId == null || item.AuthorUserId == request.AuthorUserId)
                  && (request.Level == null || item.Level >= request.Level)
              )
              .ApplyPagination(request.Pagination)
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
        .ToArrayAsync(transaction.CancellationToken);

      return new() { FileAccesses = [.. fileAccesses.ToJson()] };
    };
}
