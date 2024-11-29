using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

using System;
using Resources;

public sealed partial class Connection
{
  private sealed record class SetFileAccessRequest : BaseFileRequest
  {
    public required ObjectId? TargetUserId;
    public required FileAccessLevel Level;
  }

  private sealed record class SetFileAccessResponse { }

  private FileRequestHandler<SetFileAccessRequest, SetFileAccessResponse> SetFileAccess =>
    async (transaction, request, userAuthentication, me, myAdminAccess, fileAccess) =>
    {
      Resource<FileAccess>? fileaccess = await Resources
        .Query<FileAccess>(
          transaction,
          (query) =>
            query.Where(
              (fileAccess) =>
                fileAccess.TargetUserId == request.TargetUserId
                && fileAccess.FileId == request.FileId
            )
        )
        .FirstOrDefaultAsync(transaction.CancellationToken);

      if (fileaccess != null)
      {
        await Resources.Delete(transaction, fileaccess);
      }

      if (request.Level != FileAccessLevel.None)
      {
        await Resources.CreateFileLog(
          transaction,
          fileAccess.UnlockedFile.File,
          me,
          FileLogType.Share
        );

        if (request.TargetUserId == null)
        {
          await Resources.CreateFileAccess(transaction, fileAccess.UnlockedFile, request.Level, me);
        }
        else
        {
          Resource<User> targetUser = await Internal_EnsureFirst(
            transaction,
            Resources.Query<User>(
              transaction,
              (query) => query.Where((user) => user.Id == request.TargetUserId)
            )
          );

          await Resources.CreateFileAccess(
            transaction,
            file: fileAccess.UnlockedFile,
            level: request.Level,
            targetUser: targetUser,
            authorUser: me
          );
        }
      }
      else
      {
        await Resources.CreateFileLog(
          transaction,
          fileAccess.UnlockedFile.File,
          me,
          FileLogType.Unshare
        );
      }

      return new() { };
    };
}
