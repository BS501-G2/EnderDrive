using System.IO;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

using Resources;

public sealed partial class Connection
{
  private sealed record class FileCreateRequest : BaseFileRequest
  {
    public required string Name;
  }

  private sealed record class FileCreateResponse
  {
    public required ObjectId StreamId;
  }

  private FileRequestHandler<FileCreateRequest, FileCreateResponse> FileCreate =>
    async (transaction, request, userAuthentication, me, myAdminAccess, fileAccess) =>
    {
      FileNameValidationFlags flags = await Resources.ValidateFileName(
        transaction,
        request.Name,
        fileAccess.UnlockedFile.File
      );

      if (flags != FileNameValidationFlags.OK)
      {
        throw new FileNameConflictException()
        {
          Name = request.Name,
          ParentFileId = fileAccess.UnlockedFile.File.Id
        };
      }

      UnlockedFile file = await Resources.CreateFile(
        transaction,
        fileAccess.UnlockedFile,
        FileType.File,
        request.Name
      );

      Resource<FileData> fileData = await Resources.CreateFileData(transaction, file, null, me);

      ResourceManager.FileResourceStream stream = Resources.CreateFileStream(file, fileData, true);

      return new() { StreamId = stream.Id };
    };
}
