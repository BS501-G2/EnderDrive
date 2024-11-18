using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

using Resources;
using Utilities;

public sealed partial class Connection
{
  private sealed record class GetLatestFileSnapshotRequest : BaseFileRequest
  {
    [BsonElement("fileContentId")]
    public required ObjectId? FileContentId;
  }

  private sealed record class GetLatestFileSnapshotResponse
  {
    [BsonElement("fileSnapshot")]
    public required string? FileSnapshot;
  }

  private FileRequestHandler<
    GetLatestFileSnapshotRequest,
    GetLatestFileSnapshotResponse
  > GetLatestFileSnapshot =>
    async (
      transaction,
      request,
      userAuthentication,
      me,
      myAdminAccess,
      file,
      fileAccessResult
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

      FileSnapshot? fileSnapshot = await Resources.GetLatestFileSnapshot(
        transaction,
        file,
        fileContent
      );

      return new() { FileSnapshot = fileSnapshot?.ToJson() };
    };
}
