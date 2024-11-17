using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using RizzziGit.EnderDrive.Server.Resources;

namespace RizzziGit.EnderDrive.Server.Connections;

public sealed partial class Connection
{
  private sealed record class GetFileStarRequest
  {
    [BsonElement(
      "fileId"
    )]
    public required ObjectId FileId;
  }

  private sealed record class GetFileStarResponse
  {
    [BsonElement(
      "starred"
    )]
    public required bool Starred;
  }

  private AuthenticatedRequestHandler<
    GetFileStarRequest,
    GetFileStarResponse
  > GetFileStar =>
    async (
      transaction,
      request,
      userAuthentication,
      me,
      myAdminAccess
    ) =>
    {
      File file =
        await Internal_EnsureFirst(
          transaction,
          Resources.GetFiles(
            transaction: transaction,
            id: request.FileId
          )
        );

      FileAccessResult fileAccessResult =
        await Internal_UnlockFile(
          transaction,
          file,
          me,
          userAuthentication
        );

      return new()
      {
        Starred =
          await Resources
            .GetFileStars(
              transaction,
              file,
              me
            )
            .ToAsyncEnumerable()
            .AnyAsync(
              transaction
            ),
      };
    };
}
