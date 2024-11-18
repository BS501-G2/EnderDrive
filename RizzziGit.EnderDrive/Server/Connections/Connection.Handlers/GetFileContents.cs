using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

using Resources;
using Utilities;

public sealed partial class Connection
{
  private sealed record class GetFileContentsRequest
  {
    [BsonElement("fileId")]
    public required ObjectId? FileId;

    [BsonElement("fileContentId")]
    public required ObjectId? FileContentId;

    [BsonElement("pagination")]
    public required PaginationOptions? Pagination;
  }

  private sealed record class GetFileContentsResponse
  {
    [BsonElement("fileContents")]
    public required string[] FileContents;
  }

  private AuthenticatedRequestHandler<
    GetFileContentsRequest,
    GetFileContentsResponse
  > GetFileContents =>
    async (transaction, request, _, _, _) =>
    {
      Resource<File>? file =
        request.FileId != null
          ? await Resources
            .Query<File>(transaction, (query) => query.Where((item) => item.Id == request.FileId))
            .FirstOrDefaultAsync(transaction)
          : null;

      Resource<FileContent>[] fileContents = await Resources
        .Query<FileContent>(
          transaction,
          (query) =>
            query
              .Where(
                (item) =>
                  (file == null || item.FileId == file.Id)
                  && (request.FileContentId == null || item.Id == request.FileContentId)
              )
              .ApplyPagination(request.Pagination)
        )
        .ToArrayAsync(transaction.CancellationToken);

      return new() { FileContents = [.. fileContents.ToJson()] };
    };
}
