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
  private sealed record class GetFilesRequest
  {
    [BsonElement("searchString")]
    public required string? SearchString;

    [BsonElement("parentFolderId")]
    public required ObjectId? ParentFolderId;

    [BsonElement("fileType")]
    public required FileType? FileType;

    [BsonElement("ownerUserId")]
    public required ObjectId? OwnerUserId;

    [BsonElement("name")]
    public required string? Name;

    [BsonElement("id")]
    public required ObjectId? Id;

    [BsonElement("trashOptions")]
    public required TrashOptions? TrashOptions;

    [BsonElement("pagination")]
    public required PaginationOptions? Pagination;
  }

  private sealed record class GetFilesResponse
  {
    [BsonElement("files")]
    public required string[] Files;
  }

  private AuthenticatedRequestHandler<GetFilesRequest, GetFilesResponse> GetFiles =>
    async (transaction, request, userAuthentication, me, _) =>
    {
      Resource<File>? parentFolder = await Internal_GetFile(
        transaction,
        me,
        userAuthentication,
        request.ParentFolderId
      );

      FileAccessResult? parentFolderAccess =
        parentFolder != null
          ? await Internal_UnlockFile(
            transaction,
            parentFolder,
            me,
            userAuthentication,
            FileAccessLevel.Read
          )
          : null;

      if (
        (
          request.OwnerUserId != me.Id
          && !await Resources
            .Query<AdminAccess>(transaction, (query) => query.Where((item) => item.UserId == me.Id))
            .AnyAsync(transaction.CancellationToken)
        )
      )
      {
        throw new InvalidOperationException("Owner user is required for non-admin users");
      }

      Resource<User>? ownerUser =
        request.OwnerUserId != null
          ? await Resources
            .Query<User>(
              transaction,
              (query) => query.Where((item) => item.Id == request.OwnerUserId)
            )
            .FirstOrDefaultAsync(transaction.CancellationToken)
          : null;

      Resource<File>[] files = await Resources
        .Query<File>(
          transaction,
          (query) =>
            query
              .Where(
                (item) =>
                  (
                    request.SearchString == null
                    || item.Name.Contains(request.SearchString, StringComparison.OrdinalIgnoreCase)
                  )
                  && (parentFolder == null || item.ParentId == parentFolder.Id)
                  && (request.FileType == null || item.Type == request.FileType)
                  && (ownerUser == null || item.OwnerUserId == ownerUser.Id)
                  && (
                    request.Name == null
                    || item.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase)
                  )
                  && (request.Id == null || item.Id == request.Id)
                  && (
                    request.TrashOptions == null
                    || (
                      request.TrashOptions == TrashOptions.Exclusive
                        ? item.TrashTime != null
                        : request.TrashOptions != TrashOptions.NonInclusive
                          || item.TrashTime == null
                    )
                  )
              )
              .OrderByDescending((file) => file.Type)
              .ApplyPagination(request.Pagination)
        )
        .ToArrayAsync(transaction.CancellationToken);

      return new() { Files = [.. files.ToJson()] };
    };
}
