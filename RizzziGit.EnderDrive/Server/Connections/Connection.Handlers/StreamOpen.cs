using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using RizzziGit.EnderDrive.Server.Resources;

namespace RizzziGit.EnderDrive.Server.Connections;

public sealed partial class Connection
{
  private sealed record StreamOpenRequest : BaseFileRequest
  {
    public required ObjectId FileDataId;
    public required bool ForReading;
  }

  private sealed record StreamOpenResponse
  {
    public required ObjectId StreamId;
  }

  private FileRequestHandler<StreamOpenRequest, StreamOpenResponse> StreamOpen =>
    async (transaction, request, userAuthentication, me, myAdminAccess, fileAccess) =>
    {
      if (request.ForReading && fileAccess.AccessLevel < FileAccessLevel.ReadWrite)
      {
        throw new FileAccessForbiddenException() { FileId = fileAccess.UnlockedFile.File.Id };
      }

      Resource<FileData> fileData = await Internal_EnsureFirst(
        transaction,
        Resources.Query<FileData>(
          transaction,
          (query) =>
            query.Where(
              (fileData) => fileData.FileId == request.FileId && fileData.Id == request.FileDataId
            )
        )
      );

      ResourceManager.FileResourceStream stream = Resources.CreateFileStream(
        fileAccess.UnlockedFile,
        fileData,
        true
      );

      return new() { StreamId = stream.Id };
    };
}
