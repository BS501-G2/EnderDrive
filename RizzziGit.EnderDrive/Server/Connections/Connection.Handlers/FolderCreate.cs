using System;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

using Resources;

public sealed partial class Connection
{
  private sealed record class CreateFolderRequest : BaseFileRequest
  {
    public required string Name;
  }

  private sealed record class CreateFolderResponse
  {
    public required string File;
  }

  private FileRequestHandler<CreateFolderRequest, CreateFolderResponse> FolderCreate =>
    async (transaction, request, userAuthentication, me, myAdminAccess, fileAccess) =>
    {
      ConnectionContext context = GetContext();

      if (fileAccess.UnlockedFile.File.Data.Type != FileType.Folder)
      {
        throw new InvalidOperationException("Parent is not a folder.");
      }

      if (
        await Resources
          .Query<File>(
            transaction,
            (query) =>
              query.Where(
                (file) =>
                  file.ParentId == fileAccess.UnlockedFile.File.Id
                  && file.TrashTime == null
                  && file.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase)
              )
          )
          .AnyAsync(transaction.CancellationToken)
      )
      {
        throw new FileNameConflictException()
        {
          Name = request.Name,
          ParentFileId = request.FileId
        };
      }

      UnlockedFile newFile = await Resources.CreateFile(
        transaction,
        fileAccess.UnlockedFile,
        FileType.Folder,
        request.Name
      );

      await Resources.CreateFileLog(
        transaction,
        fileAccess.UnlockedFile.File,
        me,
        FileLogType.Update
      );
      await Resources.CreateFileLog(transaction, newFile.File, me, FileLogType.Create);

      return new() { File = newFile.File.ToJson() };
    };
}
