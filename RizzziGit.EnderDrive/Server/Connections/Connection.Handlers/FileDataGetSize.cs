using System.Linq;
using MongoDB.Bson;
using RizzziGit.EnderDrive.Server.Resources;

namespace RizzziGit.EnderDrive.Server.Connections;

public sealed partial class Connection
{
  private sealed record FileGetSizeRequest : BaseFileRequest
  {
    public required ObjectId? FileDataId;
  }

  private sealed record FileGetSizeResponse
  {
    public required long Size;
  }

  private FileRequestHandler<FileGetSizeRequest, FileGetSizeResponse> FileGetSize =>
    async (transaction, request, userAuthentication, me, myAdminAccess, fileAccess) =>
    {
      Resource<FileData> fileData = await Internal_EnsureFirst(
        transaction,
        Resources.Query<FileData>(
          transaction,
          (query) =>
            query
              .Where(
                (fileData) =>
                  (request.FileDataId == null || fileData.Id == request.FileDataId)
                  && fileData.FileId == request.FileId
              )
              .OrderByDescending((fileData) => fileData.CreateTime)
        )
      );

      return new()
      {
        Size = await Resources.GetFileSize(transaction, fileAccess.UnlockedFile, fileData)
      };
    };
}
