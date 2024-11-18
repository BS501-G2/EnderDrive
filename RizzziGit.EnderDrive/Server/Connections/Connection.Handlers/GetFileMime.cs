using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

using Resources;

public sealed partial class Connection
{
  private sealed record class GetFileMimeRequest : BaseFileRequest
  {
    [BsonElement("fileContentId")]
    public required ObjectId? FileContentId;

    [BsonElement("fileSnapshotId")]
    public required ObjectId? FileSnapshotId;
  }

  private sealed record class GetFileMimeResponse
  {
    [BsonElement("fileMimeType")]
    public required string FileMimeType;
  }

  private FileRequestHandler<
    GetFileMimeRequest,
    GetFileMimeResponse
  > GetFileMime =>
    async (
      transaction,
      request,
      userAuthentication,
      me,
      _,
      file,
      fileAccessResult
    ) =>
    {
      ConnectionContext context = GetContext();

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

      FileSnapshot fileSnapshot =
        request.FileSnapshotId != null
          ? await Internal_EnsureFirst(
            transaction,
            Resources.GetFileSnapshots(
              transaction,
              file,
              fileContent,
              request.FileSnapshotId
            )
          )
          : await Resources.GetLatestFileSnapshot(
            transaction,
            file,
            fileContent
          )
            ?? await Resources.CreateFileSnapshot(
              transaction,
              fileAccessResult.File,
              fileContent,
              userAuthentication,
              null
            );

      MimeDetective.Storage.Definition? definition =
        await Server.MimeDetector.Inspect(
          transaction,
          fileAccessResult.File,
          fileContent,
          fileSnapshot
        );

      return new()
      {
        FileMimeType = definition?.File.MimeType ?? "application/octet-stream",
      };
    };
}
