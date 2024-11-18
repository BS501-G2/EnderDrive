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
  private sealed record class GetFileSnapshotsRequest
  {
    [BsonElement("fileId")]
    public required ObjectId? FileId;

    [BsonElement("fileContentId")]
    public required ObjectId? FileContentId;

    [BsonElement("fileSnapshotId")]
    public required ObjectId? FileSnapshotId;

    [BsonElement("pagination")]
    public required PaginationOptions? Pagination;
  }

  private sealed record class GetFileSnapshotsResponse
  {
    [BsonElement("fileSnapshots")]
    public required string[] FileSnapshots;
  }

  private AuthenticatedRequestHandler<
    GetFileSnapshotsRequest,
    GetFileSnapshotsResponse
  > GetFileSnapshots =>
    async (transaction, request, userAuthentication, me, myAdminAccess) =>
    {
      ConnectionContext context = GetContext();

      Resource<File>? file =
        request.FileId != null
          ? await Resources
            .Query<File>(transaction, (query) => query.Where((item) => item.Id == request.FileId))
            .FirstOrDefaultAsync(transaction.CancellationToken)
          : null;

      Resource<FileContent>? fileContent =
        request.FileContentId != null
          ? await Resources
            .Query<FileContent>(
              transaction,
              (fileContent) =>
                fileContent.Where(
                  (item) =>
                    (file == null || item.FileId == file.Id)
                    && (request.FileContentId == null || item.Id == request.FileContentId)
                )
            )
            .FirstOrDefaultAsync(transaction.CancellationToken)
          : null;

      Resource<FileSnapshot>[] fileSnapshots = await Resources
        .Query<FileSnapshot>(
          transaction,
          (query) =>
            query
              .Where(
                (fileSnapshot) =>
                  (fileContent == null || fileSnapshot.FileContentId == fileContent.Id)
                  && (request.FileSnapshotId == null || fileSnapshot.Id == request.FileSnapshotId)
              )
              .ApplyPagination(request.Pagination)
        )
        .ToArrayAsync(transaction.CancellationToken);

      return new() { FileSnapshots = [.. fileSnapshots.ToJson()] };
    };
}
