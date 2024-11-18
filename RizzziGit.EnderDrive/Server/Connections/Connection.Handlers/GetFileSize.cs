using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

using Resources;

public sealed partial class Connection
{
  private sealed record class GetFileSizeRequest : BaseFileRequest
  {
    [BsonElement("fileContentId")]
    public required ObjectId? FileContentId;

    [BsonElement("fileSnapshotId")]
    public required ObjectId? FileSnapshotId;
  }

  private sealed record class GetFileSizeResponse
  {
    [BsonElement("size")]
    public required long Size;
  }

  private FileRequestHandler<GetFileSizeRequest, GetFileSizeResponse> GetFileSize =>
    async (transaction, request, userAuthentication, me, _, fileAccessResult) =>
    {
      Resource<FileContent> fileContent =
        request.FileContentId != null
          ? await Internal_EnsureFirst(
            transaction,
            Resources.Query<FileContent>(
              transaction,
              (query) =>
                query.Where(
                  (item) =>
                    item.FileId == fileAccessResult.UnlockedFile.File.Id
                    && item.Id == request.FileContentId
                )
            )
          )
          : await Resources.GetMainFileContent(transaction, fileAccessResult.UnlockedFile.File);

      Resource<FileSnapshot>? fileSnapshot =
        request.FileSnapshotId != null
          ? await Resources
            .Query<FileSnapshot>(
              transaction,
              (query) =>
                query.Where(
                  (item) =>
                    item.FileId == fileAccessResult.UnlockedFile.File.Id
                    && item.FileContentId == fileContent.Id
                    && item.Id == request.FileSnapshotId
                )
            )
            .FirstOrDefaultAsync(transaction.CancellationToken)
          : await Resources
            .Query<FileSnapshot>(
              transaction,
              (query) =>
                query.Where(
                  (item) =>
                    item.FileId == fileContent.Data.FileId && item.FileContentId == fileContent.Id
                )
            )
            .OrderByDescending((item) => item.Data.CreateTime)
            .FirstOrDefaultAsync(transaction.CancellationToken);

      return new() { Size = fileSnapshot?.Data.Size ?? 0 };
    };
}
