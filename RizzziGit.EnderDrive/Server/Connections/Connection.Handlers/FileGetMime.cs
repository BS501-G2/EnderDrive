using System.Linq;
using MimeDetective.Storage;
using MongoDB.Bson;
using RizzziGit.EnderDrive.Server.Resources;

namespace RizzziGit.EnderDrive.Server.Connections;

public sealed partial class Connection
{
  private sealed record class FileGetMimeRequest : BaseFileRequest
  {
    public required ObjectId? FileDataId;
  }

  private sealed record class FileGetMimeResponse
  {
    public required string MimeType;
  }

  private FileRequestHandler<FileGetMimeRequest, FileGetMimeResponse> FileGetMime =>
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
                  fileData.FileId == request.FileId
                  && (request.FileDataId == null || fileData.Id == request.FileDataId)
              )
              .OrderByDescending((fileData) => fileData.CreateTime)
        )
      );

      Definition? report = await Server.MimeDetector.Inspect(
        transaction,
        fileAccess.UnlockedFile,
        fileData
      );

      return new() { MimeType = report?.File.MimeType ?? "application/octet-stream" };
    };
}
