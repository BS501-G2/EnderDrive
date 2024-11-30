namespace RizzziGit.EnderDrive.Server.Connections;

using System;
using Resources;

public sealed partial class Connection
{
  private sealed record class TrashFileRequest : BaseFileRequest { }

  private sealed record class TrashFileResponse { }

  private FileRequestHandler<TrashFileRequest, TrashFileResponse> TrashFile =>
    async (transaction, request, userAuthentication, me, myAdminAccess, fileAccess) =>
    {
      Resource<File> parentFolder = await Internal_GetFile(transaction, me, userAuthentication, fileAccess.UnlockedFile.File.Id);

      fileAccess.UnlockedFile.File.Data.TrashTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
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
        FileLogType.Trash
      );

      return new() { };
    };
}
