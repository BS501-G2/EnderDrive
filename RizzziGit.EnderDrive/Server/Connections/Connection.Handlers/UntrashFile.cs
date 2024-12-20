namespace RizzziGit.EnderDrive.Server.Connections;

using System;
using System.Linq;
using Resources;

public sealed partial class Connection
{
  private sealed record class UntrashFileRequest : BaseFileRequest { }

  private sealed record class UntrashFileResponse { }

  private FileRequestHandler<UntrashFileRequest, UntrashFileResponse> UntrashFile =>
    async (transaction, request, userAuthentication, me, myAdminAccess, fileAccess) =>
    {
      Resource<File> parentFolder = await Internal_GetFile(
        transaction,
        me,
        userAuthentication,
        fileAccess.UnlockedFile.File.Id
      );

      if (
        await Resources
          .Query<File>(
            transaction,
            query =>
              query.Where(
                (file) =>
                  file.ParentId == parentFolder.Id
                  && file.Name.Equals(
                    fileAccess.UnlockedFile.File.Data.Name,
                    StringComparison.CurrentCultureIgnoreCase
                  )
                  && file.TrashTime == null
              )
          )
          .AnyAsync(transaction)
      )
      {
        throw new FileNameConflictException()
        {
          Name = fileAccess.UnlockedFile.File.Data.Name,
          ParentFileId = fileAccess.UnlockedFile.File.Id
        };
      }

      fileAccess.UnlockedFile.File.Data.TrashTime = null;
      await fileAccess.UnlockedFile.File.Save(transaction);

      await Resources.CreateFileLog(
        transaction,
        fileAccess.UnlockedFile.File,
        me,
        FileLogType.Update
      );

      await Resources.CreateFileLog(
        transaction,
        fileAccess.UnlockedFile.File,
        me,
        FileLogType.Untrash
      );

      await Internal_BroadcastFileActivity(
        transaction,
        me,
        fileAccess.UnlockedFile,
        fileAccess.FileAccess,
        new NotificationData.File.FileRestore() { FileId = fileAccess.UnlockedFile.File.Id, }
      );

      return new() { };
    };
}
