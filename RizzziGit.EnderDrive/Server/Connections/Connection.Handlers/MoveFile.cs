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
    public required ObjectId? NewParentId;

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

      Resource<File>? newParent =
        request.NewParentId != null
          ? await Internal_GetFile(transaction, me, userAuthentication, request.NewParentId)
          : null;

      FileAccessResult? newParentAccessResult = null;
      if (newParent != null)
      {
        if (newParent.Data.Type != FileType.Folder)
        {
          throw new InvalidOperationException("Parent is not a folder.");
        }

        newParentAccessResult = await Internal_UnlockFile(
          transaction,
          newParent,
          me,
          userAuthentication
        );
      }

      string newName = request.NewName ?? fileAccess.UnlockedFile.File.Data.Name;

      if (
        await Resources
          .Query<File>(
            transaction,
            (query) =>
              query.Where(
                (file) =>
                  file.ParentId == (newParent != null ? newParent.Id : oldParent.Id)
                  && file.TrashTime == null
                  && file.Name.Equals(newName, StringComparison.OrdinalIgnoreCase)
              )
          )
          .AnyAsync(transaction.CancellationToken)
      )
      {
        throw new ConnectionResponseException(
          ResponseCode.FileNameConflict,
          new ConnectionResponseExceptionData.FileNameConflict() { Name = newName }
        );
      }

      if (newParentAccessResult != null)
      {
        await Resources.MoveFile(
          transaction,
          fileAccess.UnlockedFile,
          newParentAccessResult.UnlockedFile
        );
      }

      fileAccess.UnlockedFile.File.Data.Name = newName;
      await fileAccess.UnlockedFile.File.Save(transaction);

      if (newParent != null)
      {
        await Resources.CreateFileLog(transaction, newParent, me, FileLogType.Update);
      }

      await Resources.CreateFileLog(transaction, oldParent, me, FileLogType.Update);

      return new() { };
    };
}
