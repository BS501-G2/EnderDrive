using System.IO;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

using Resources;

public sealed partial class Connection
{
  private sealed record class SetFileStarRequest
  {
    [BsonElement(
      "fileId"
    )]
    public required ObjectId FileId;

    [BsonElement(
      "starred"
    )]
    public required bool Starred;
  }

  private sealed record class SetFileStarResponse { }

  private AuthenticatedRequestHandler<
    SetFileStarRequest,
    SetFileStarResponse
  > SetFileStar =>
    async (
      transaction,
      request,
      userAuthentication,
      me,
      _
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
          userAuthentication,
          FileAccessLevel.Read
        );

      await Resources.SetFileStar(
        transaction,
        fileAccessResult.File,
        me,
        request.Starred
      );
      return new()
      { };
    };
}
