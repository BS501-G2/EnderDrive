using System.Linq;

namespace RizzziGit.EnderDrive.Server.Connections;

using Resources;

public sealed partial class Connection
{
  private sealed record class DeleteFileRequest : BaseFileRequest { }

  private sealed record class DeleteFileResponse { }

  private FileRequestHandler<DeleteFileRequest, DeleteFileResponse> FileDelete =>
    async (transaction, request, userAuthentication, me, myAdminAccess, fileAccess) =>
    {
      await Resources.Delete(transaction, fileAccess.UnlockedFile.File);

      await Internal_BroadcastFileActivity(
        transaction,
        me,
        fileAccess.UnlockedFile,
        fileAccess.FileAccess,
        new NotificationData.File.FileDelete() { FileId = fileAccess.UnlockedFile.File.Id }
      );

      return new() { };
    };
}
