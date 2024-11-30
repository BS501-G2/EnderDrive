using System.Linq;
using MongoDB.Bson;
using RizzziGit.EnderDrive.Server.Resources;

namespace RizzziGit.EnderDrive.Server.Connections;

public sealed partial class Connection
{
  private sealed record FileDataGetSizeRequest : BaseFileRequest
  {
    public required ObjectId FileDataId;
  }

  private sealed record FileDataGetSizeResponse
  {
    public required long Size;
  }

  private FileRequestHandler<FileDataGetSizeRequest, FileDataGetSizeResponse> FileDataGetSize =>
    async (transaction, request, userAuthentication, me, myAdminAccess, fileAccess) =>
    {
      Resource<FileData> fileData = await Internal_EnsureFirst(
        transaction,
        Resources.Query<FileData>(
          transaction,
          (query) =>
            query.Where(
              (fileData) => fileData.Id == request.FileDataId && fileData.FileId == request.FileId
            )
        )
      );

      return new() {
        Size = fileData.Data.Size
      };
    };
}
