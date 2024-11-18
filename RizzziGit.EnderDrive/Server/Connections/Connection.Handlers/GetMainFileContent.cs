using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

using Server.Resources;
using Utilities;

public sealed partial class Connection
{
  private sealed record class GetMainFileContentRequest : BaseFileRequest { }

  private sealed record class GetMainFileContentResonse
  {
    [BsonElement("fileContent")]
    public required string FileContent;
  }

  private FileRequestHandler<
    GetMainFileContentRequest,
    GetMainFileContentResonse
  > GetMainFileContent =>
    async (
      transaction,
      request,
      userAuthentication,
      me,
      myAdminAccess,
      file,
      fileAccessResult
    ) =>
    {
      FileContent fileContent = await Resources.GetMainFileContent(
        transaction,
        fileAccessResult.File
      );
      return new() { FileContent = fileContent.ToJson() };
    };
}
