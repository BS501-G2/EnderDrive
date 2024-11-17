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
    [BsonElement(
      "fileId"
    )]
    public required ObjectId? FileId;

    [BsonElement(
      "userId"
    )]
    public required ObjectId? UserId;

    [BsonElement(
      "pagination"
    )]
    public required PaginationOptions? Pagination;
  };

  private sealed record class GetFileStarsResponse
  {
    [BsonElement(
      "fileStars"
    )]
    public required string[] FileStars;
  };

  private AuthenticatedRequestHandler<
    GetFileStarsRequest,
    GetFileStarsResponse
  > GetFileStars =>
    async (
      transaction,
      request,
      userAuthentication,
      me,
      myAdminAccess
    ) =>
    {
      User? user =
        null;

      if (
        request.UserId
          != me.Id
        && myAdminAccess
          == null
      )
      {
        throw new InvalidOperationException(
          "User must be set to self when not an admin."
        );
      }

      user =
        request.UserId
        != null
          ? await Internal_EnsureFirst(
            transaction,
            Resources.GetUsers(
              transaction,
              id: request.UserId
            )
          )
          : null;

      File? file =
        request.FileId
        != null
          ? await Internal_EnsureFirst(
            transaction,
            Resources.GetFiles(
              transaction,
              id: request.FileId
            )
          )
          : null;

      FileAccessResult? fileAccess =
        file
        != null
          ? await Resources.FindFileAccess(
            transaction,
            file,
            me,
            userAuthentication,
            FileAccessLevel.Read
          )
          : null;

      FileStar[] fileStars =
        await Resources
          .GetFileStars(
            transaction,
            file,
            user
          )
          .ApplyPagination(
            request.Pagination
          )
          .ToAsyncEnumerable()
          .WhereAwait(
            async (
              fileStar
            ) =>
            {
              File file =
                await Internal_GetFile(
                  transaction,
                  me,
                  request.FileId
                );

              FileAccessResult? fileAccessResult =
                await Resources.FindFileAccess(
                  transaction,
                  file,
                  me,
                  userAuthentication,
                  FileAccessLevel.Read
                );

              return fileAccessResult
                != null;
            }
          )
          .ToArrayAsync(
            transaction.CancellationToken
          );

      return new()
      {
        FileStars =
          fileStars
            .Select(
              (
                fileStar
              ) =>
                JToken
                  .FromObject(
                    fileStar
                  )
                  .ToString()
            )
            .ToArray(),
      };
    };
}
