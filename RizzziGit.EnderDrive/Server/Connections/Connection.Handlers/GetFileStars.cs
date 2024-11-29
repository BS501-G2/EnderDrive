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
  private sealed record class GetFileStarsRequest
  {
    public required ObjectId? FileId;
    public required ObjectId? UserId;
    public required PaginationOptions? Pagination;
  };

  private sealed record class GetFileStarsResponse
  {
    public required string[] FileStars;
  };

  private AuthenticatedRequestHandler<GetFileStarsRequest, GetFileStarsResponse> GetFileStars =>
    async (transaction, request, userAuthentication, me, myAdminAccess) =>
    {
      Resource<User>? user = null;

      if (request.UserId != me.Id && myAdminAccess == null)
      {
        throw new InvalidOperationException("User must be set to self when not an admin.");
      }

      user =
        request.UserId != null
          ? await Internal_EnsureFirst(
            transaction,
            Resources.Query<User>(
              transaction,
              (query) => query.Where((user) => user.Id == request.UserId)
            )
          )
          : null;

      Resource<File>? file =
        request.FileId != null
          ? await Internal_EnsureFirst(
            transaction,
            Resources.Query<File>(
              transaction,
              (query) => query.Where((file) => file.Id == request.FileId)
            )
          )
          : null;

      FileAccessResult? fileAccess =
        file != null
          ? await Resources.FindFileAccess(
            transaction,
            file,
            me,
            userAuthentication,
            FileAccessLevel.Read
          )
          : null;

      Resource<FileStar>[] fileStars = await Resources
        .Query<FileStar>(
          transaction,
          (query) =>
            query
              .Where(
                (item) =>
                  (file == null || item.FileId == file.Id)
                  && (user == null || item.UserId == user.Id)
                  && item.Starred
              )
              .ApplyPagination(request.Pagination)
        )
        .WhereAwait(
          async (fileStar) =>
          {
            Resource<File> file = await Internal_GetFile(
              transaction,
              me,
              userAuthentication,
              request.FileId
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

      return new() { FileStars = [.. fileStars.ToJson()] };
    };
}
