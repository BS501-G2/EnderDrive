using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

using System.Linq;
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
    async (transaction, request, userAuthentication, me, myAdminAccess, fileAccessResult) =>
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
                    item.Id == request.FileContentId
                    && (request.FileContentId == null || item.Id == request.FileContentId)
                )
            )
          )
          : await Resources.GetMainFileContent(transaction, fileAccessResult.UnlockedFile.File);

      Resource<FileSnapshot>? fileSnapshot = await Resources
        .Query<FileSnapshot>(
          transaction,
          (query) =>
            query
              .Where(
                (item) =>
                  item.FileContentId == fileContent.Id
                  && item.FileId == fileAccessResult.UnlockedFile.File.Id
              )
              .OrderByDescending((item) => item.CreateTime)
        )
        .FirstOrDefaultAsync(transaction.CancellationToken);

      return new() { FileSnapshot = fileSnapshot?.ToJson() };
    };
}
