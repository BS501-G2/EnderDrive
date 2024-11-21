using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

using Resources;

public sealed partial class Connection
{
  private sealed record class SetFileAccessRequest : BaseFileRequest
  {
    [BsonElement("targetUserId")]
    public required ObjectId? TargetUserId;

    [BsonElement("level")]
    public required FileAccessLevel Level;
  }

  private sealed record class SetFileAccessResponse { }

  private FileRequestHandler<SetFileAccessRequest, SetFileAccessResponse> SetFileAccess =>
    async (transaction, request, userAuthentication, me, myAdminAccess, fileAccess) =>
    {
      Resource<FileAccess>? fileaccess = await Resources
        .Query<FileAccess>(
          transaction,
          (query) => query.Where((fileAccess) => fileAccess.TargetUserId == request.TargetUserId)
        )
        .FirstOrDefaultAsync(transaction.CancellationToken);

      if (fileaccess != null)
      {
        await Resources.Delete(transaction, fileaccess);
      }

      if (request.Level != FileAccessLevel.None)
      {
        if (request.TargetUserId == null)
        {
          await Resources.CreateFileAccess(transaction, fileAccess.UnlockedFile, request.Level, me);
        }
        else
        {
          Resource<User> resource = await Internal_EnsureFirst(
            transaction,
            Resources.Query<User>(
              transaction,
              (query) => query.Where((user) => user.Id == request.TargetUserId)
            )
          );

          await Resources.CreateFileAccess(
            transaction,
            fileAccess.UnlockedFile,
            resource,
            me,
            request.Level
          );
        }
      }

      return new() { };
    };
}
