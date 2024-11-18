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
    [BsonElement("parentFolderId")]
    public required ObjectId? ParentFolderId;

    [BsonElement("fileType")]
    public required FileType? FileType;

    [BsonElement("name")]
    public required string? Name;

    [BsonElement("ownerUserId")]
    public required ObjectId? OwnerUserId;

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

  private AuthenticatedRequestHandler<
    GetFilesRequest,
    GetFilesResponse
  > GetFiles =>
    async (transaction, request, userAuthentication, me, _) =>
    {
      File? parentFolder = await Internal_GetFile(
        transaction,
        me,
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

      User? ownerUser =
        request.OwnerUserId != null
          ? await Resources
            .GetUsers(transaction, id: request.OwnerUserId)
            .ToAsyncEnumerable()
            .FirstOrDefaultAsync(transaction.CancellationToken)
          : null;

      if (
        ownerUser == null
        || (
          ownerUser.Id != me.Id
          && !await Resources
            .GetAdminAccesses(transaction, userId: me.Id)
            .ToAsyncEnumerable()
            .AnyAsync(transaction.CancellationToken)
        )
      )
      {
        throw new InvalidOperationException(
          "Owner user is not required is required for non-admin users"
        );
      }

      File[] files = await Resources
        .GetFiles(
          transaction,
          parentFolder,
          request.FileType,
          request.Name,
          ownerUser,
          request.Id,
          request.TrashOptions ?? TrashOptions.NotIncluded
        )
        .OrderByDescending((file) => file.Type)
        .ApplyPagination(request.Pagination)
        .ToAsyncEnumerable()
        .ToArrayAsync(transaction.CancellationToken);

      return new()
      {
        Files = files
          .Select((file) => JToken.FromObject(file).ToString())
          .ToArray(),
      };
    };
}
