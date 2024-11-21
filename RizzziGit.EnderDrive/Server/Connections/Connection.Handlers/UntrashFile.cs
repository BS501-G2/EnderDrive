namespace RizzziGit.EnderDrive.Server.Connections;

using System;
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

      return new() { };
    };
}
