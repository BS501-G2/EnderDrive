using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

using System.Linq;
using Resources;
using Utilities;

public sealed partial class Connection
{
  private sealed record class GetOldestFileSnapshotRequest : BaseFileRequest
  {
    [BsonElement("fileContentId")]
    public required ObjectId? FileContentId;
  }

  private sealed record class GetOldestFileSnapshotResponse
  {
    [BsonElement("fileSnapshot")]
    public required string? FileSnapshot;
  }

  private FileRequestHandler<
    GetOldestFileSnapshotRequest,
    GetOldestFileSnapshotResponse
  > GetOldestFileSnapshot =>
    async (
      transaction,
      request,
      userAuthentication,
      me,
      myAdminAccess,
      file,
      fileAccess
    ) =>
    {
      FileContent fileContent =
        request.FileContentId != null
          ? await Internal_EnsureFirst(
            transaction,
            Resources.GetFileContents(
              transaction,
              file,
              id: request.FileContentId
            )
          )
          : await Resources.GetMainFileContent(transaction, file);

      FileSnapshot? fileSnapshot = await Internal_GetFirst(
        transaction,
        Resources
          .GetFileSnapshots(transaction, file, fileContent)
          .OrderBy((item) => item.Id)
      );

      return new() { FileSnapshot = fileSnapshot?.ToJson() };
    };
}
