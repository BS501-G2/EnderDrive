using System;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

using Resources;

public sealed partial class Connection
{
  private sealed record MoveFileRequest : BaseFileRequest
  {
    [BsonElement("newParentId")]
    public required ObjectId NewParentId;

    [BsonElement("newName")]
    public required string? NewName;
  }

  private sealed record MoveFileResponse { }

  private FileRequestHandler<MoveFileRequest, MoveFileResponse> MoveFile =>
    async (transaction, request, userAuthentication, me, myAdminAccess, fileAccess) =>
    {
      Resource<File> oldParent = await Internal_GetFile(
        transaction,
        me,
        userAuthentication,
        fileAccess.UnlockedFile.File.Data.ParentId
      );

      Resource<File> newParent = await Internal_GetFile(
        transaction,
        me,
        userAuthentication,
        request.NewParentId
      );

      if (newParent.Data.Type != FileType.Folder)
      {
        throw new InvalidOperationException("Parent is not a folder.");
      }

      FileAccessResult newParentAccessResult = await Internal_UnlockFile(
        transaction,
        newParent,
        me,
        userAuthentication
      );

      string newName = request.NewName ?? fileAccess.UnlockedFile.File.Data.Name;

      if (
        await Resources
          .Query<File>(
            transaction,
            (query) =>
              query
                .Where((file) => file.ParentId == newParent.Id)
                .Where((file) => file.TrashTime != null)
                .Where((file) => file.Name.Equals(newName, StringComparison.OrdinalIgnoreCase))
          )
          .AnyAsync(transaction.CancellationToken)
      )
      {
        throw new ConnectionResponseException(
          ResponseCode.FileNameConflict,
          new ConnectionResponseExceptionData.FileNameConflict() { Name = newName }
        );
      }

      await Resources.MoveFile(
        transaction,
        fileAccess.UnlockedFile,
        newParentAccessResult.UnlockedFile
      );

      fileAccess.UnlockedFile.File.Data.Name = newName;
      await fileAccess.UnlockedFile.File.Save(transaction);

      await Resources.CreateFileLog(transaction, newParent, me, FileLogType.Update);
      await Resources.CreateFileLog(transaction, oldParent, me, FileLogType.Update);

      return new() { };
    };
}
