using System.Linq;
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

  private FileRequestHandler<GetFileMimeRequest, GetFileMimeResponse> GetFileMime =>
    async (transaction, request, userAuthentication, me, _, fileAccessResult) =>
    {
      ConnectionContext context = GetContext();

      Resource<FileContent> fileContent =
        request.FileContentId != null
          ? await Internal_EnsureFirst(
            transaction,
            Resources.Query<FileContent>(
              transaction,
              (query) => query.Where((item) => item.Id == request.FileContentId)
            )
          )
          : await Resources.GetMainFileContent(transaction, fileAccessResult.UnlockedFile.File);

      Resource<FileSnapshot> fileSnapshot =
        request.FileSnapshotId != null
          ? await Internal_EnsureFirst(
            transaction,
            Resources.Query<FileSnapshot>(
              transaction,
              (query) =>
                query.Where(
                  (item) =>
                    item.FileId == fileAccessResult.UnlockedFile.File.Id
                    && item.FileContentId == fileContent.Id
                    && item.Id == request.FileSnapshotId
                )
            )
          )
          : await Resources
            .Query<FileSnapshot>(
              transaction,
              (query) =>
                query
                  .Where((item) => item.FileId == fileAccessResult.UnlockedFile.File.Id)
                  .OrderByDescending((item) => item.CreateTime)
            )
            .FirstOrDefaultAsync(transaction.CancellationToken)
            ?? await Resources.CreateFileSnapshot(
              transaction,
              fileAccessResult.UnlockedFile,
              fileContent,
              userAuthentication,
              null
            );

      MimeDetective.Storage.Definition? definition = await Server.MimeDetector.Inspect(
        transaction,
        fileAccessResult.UnlockedFile,
        fileContent,
        fileSnapshot
      );

      return new() { FileMimeType = definition?.File.MimeType ?? "application/octet-stream" };
    };
}
