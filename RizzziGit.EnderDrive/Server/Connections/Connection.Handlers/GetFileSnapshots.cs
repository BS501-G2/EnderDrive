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
  private sealed record class GetFileSnapshotsRequest : BaseFileRequest
  {
    [BsonElement("fileContentId")]
    public required ObjectId? FileContentId;

    [BsonElement("pagination")]
    public required PaginationOptions? Pagination;
  }

  private sealed record class GetFileSnapshotsResponse
  {
    [BsonElement("fileSnapshots")]
    public required string[] FileSnapshots;
  }

  private FileRequestHandler<
    GetFileSnapshotsRequest,
    GetFileSnapshotsResponse
  > GetFileSnapshots =>
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
        await Resources
          .GetFileContents(transaction, file, id: request.FileContentId)
          .ToAsyncEnumerable()
          .FirstOrDefaultAsync(transaction.CancellationToken)
        ?? throw new InvalidOperationException("Invalid file content id.");

      FileSnapshot[] fileSnapshots = await Resources
        .GetFileSnapshots(transaction, file, fileContent)
        .ApplyPagination(request.Pagination)
        .ToAsyncEnumerable()
        .ToArrayAsync(transaction.CancellationToken);

      return new()
      {
        FileSnapshots =
        [
          .. fileSnapshots.Select(
            (fileSnapshot) => JToken.FromObject(fileSnapshot).ToString()
          ),
        ],
      };
    };
}
