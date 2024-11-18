namespace RizzziGit.EnderDrive.Server.Connections;

public sealed partial class Connection
{
  private sealed record class DeleteFileRequest : BaseFileRequest { }

  private sealed record class DeleteFileResponse { }

  private FileRequestHandler<
    DeleteFileRequest,
    DeleteFileResponse
  > DeleteFile =>
    async (
      transaction,
      request,
      userAuthentication,
      me,
      myAdminAccess,
      file,
      fileAccess
    ) =>
    {
      await Resources.Delete(transaction, fileAccess.File);

      return new() { };
    };
}
