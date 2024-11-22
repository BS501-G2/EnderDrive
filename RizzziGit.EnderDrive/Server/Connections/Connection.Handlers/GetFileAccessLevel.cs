using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using RizzziGit.EnderDrive.Server.Resources;

namespace RizzziGit.EnderDrive.Server.Connections;

public sealed partial class Connection
{
  private sealed record class GetFileAccessLevelRequest
  {
    [BsonElement("fileId")]
    public required ObjectId FileId;
  }

  private sealed record class GetFileAccessLevelResponse
  {
    [BsonElement("fileAccessLevel")]
    public required FileAccessLevel FileAccessLevel;
  }

  private AuthenticatedRequestHandler<
    GetFileAccessLevelRequest,
    GetFileAccessLevelResponse
  > GetFileAccessLevel =>
    async (transaction, request, userAuthentication, me, myAdminAccess) =>
    {
      Resource<File> file = await Internal_EnsureFirst(
        transaction,
        Resources.Query<File>(
          transaction,
          (query) => query.Where((item) => item.Id == request.FileId)
        )
      );

      FileAccessResult? fileAccessResult = await Resources.FindFileAccess(
        transaction,
        file,
        me,
        userAuthentication,
        FileAccessLevel.None
      );

      return new() { FileAccessLevel = fileAccessResult?.AccessLevel ?? FileAccessLevel.None };
    };
}
