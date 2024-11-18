using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

public sealed partial class Connection
{
  private sealed record class GetFileRequest : BaseFileRequest { }

  private sealed record class GetFileResponse
  {
    [BsonElement("file")]
    public required string File;
  }

  private static FileRequestHandler<GetFileRequest, GetFileResponse> GetFile =>
    async (_, _, _, _, _, fileAccessResult) =>
      new() { File = fileAccessResult.UnlockedFile.File.ToJson() };
}
