using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;

namespace RizzziGit.EnderDrive.Server.Connections;

public sealed partial class Connection
{
  private sealed record class GetFileRequest
    : BaseFileRequest { }

  private sealed record class GetFileResponse
  {
    [BsonElement(
      "file"
    )]
    public required string File;
  }

  private FileRequestHandler<
    GetFileRequest,
    GetFileResponse
  > GetFile =>
    async (
      transaction,
      request,
      userAuthentication,
      me,
      _,
      file,
      fileAccessResult
    ) =>
      new()
      {
        File =
          JToken
            .FromObject(
              file
            )
            .ToString(),
      };
}
