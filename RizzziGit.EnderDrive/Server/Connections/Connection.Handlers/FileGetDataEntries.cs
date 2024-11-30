using System.Linq;
using MongoDB.Bson;

namespace RizzziGit.EnderDrive.Server.Connections;

using Resources;
using Utilities;

public sealed partial class Connection
{
  private sealed record FileGetDataEntriesRequest : BaseFileRequest
  {
    public required ObjectId? FileDataId;

    public required PaginationOptions? Pagination;
  }

  private sealed record FileGetDataEntriesResponse
  {
    public required string[] FileDataEntries;
  }

  private FileRequestHandler<
    FileGetDataEntriesRequest,
    FileGetDataEntriesResponse
  > FileGetDataEntries =>
    async (transaction, request, userAuthentication, me, myAdminAccess, fileAccess) =>
    {
      Resource<FileData>[] fileData = await Resources
        .Query<FileData>(
          transaction,
          (query) =>
            query
              .Where(
                (fileData) =>
                  fileData.FileId == request.FileId
                  && (request.FileDataId == null || request.FileDataId == fileData.Id)
              )
              .OrderByDescending((fileData) => fileData.Id)
              .ApplyPagination(request.Pagination)
        )
        .ToArrayAsync(transaction);

      return new() { FileDataEntries = [.. fileData.ToJson()] };
    };
}
