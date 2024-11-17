using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;

namespace RizzziGit.EnderDrive.Server.Connections;

using Resources;
using Utilities;

public sealed partial class Connection
{
  private sealed record class GetFileContentsRequest
    : BaseFileRequest
  {
    [BsonElement(
      "pagination"
    )]
    public required PaginationOptions? Pagination;
  }

  private sealed record class GetFileContentsResponse
  {
    [BsonElement(
      "fileContents"
    )]
    public required string[] FileContents;
  }

  private FileRequestHandler<
    GetFileContentsRequest,
    GetFileContentsResponse
  > GetFileContents =>
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
      FileContent[] fileContents =
        await Resources
          .GetFileContents(
            transaction,
            file
          )
          .ApplyPagination(
            request.Pagination
          )
          .ToAsyncEnumerable()
          .ToArrayAsync(
            transaction.CancellationToken
          );

      return new()
      {
        FileContents =
          fileContents
            .Select(
              (
                fileContent
              ) =>
                JToken
                  .FromObject(
                    fileContent
                  )
                  .ToString()
            )
            .ToArray(),
      };
    };
}
