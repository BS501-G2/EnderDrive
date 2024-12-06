using System;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using RizzziGit.EnderDrive.Server.Resources;

namespace RizzziGit.EnderDrive.Server.Connections;

public sealed partial class Connection
{
  private sealed record StreamOpenRequest : BaseFileRequest
  {
    public required ObjectId FileDataId;
    public required bool ForWriting;
  }

  private sealed record StreamOpenResponse
  {
    public required ObjectId StreamId;
  }

  private FileRequestHandler<StreamOpenRequest, StreamOpenResponse> StreamOpen =>
    async (transaction, request, userAuthentication, me, myAdminAccess, fileAccess) =>
    {
      if (request.ForWriting && fileAccess.AccessLevel < FileAccessLevel.ReadWrite)
      {
        throw new FileAccessForbiddenException() { FileId = fileAccess.UnlockedFile.File.Id };
      }

      Resource<FileData> fileData = await Internal_EnsureFirst(
        transaction,
        Resources.Query<FileData>(
          transaction,
          (query) =>
            query.Where(
              (fileData) => fileData.FileId == request.FileId && fileData.Id == request.FileDataId
            )
        )
      );

      if (request.ForWriting)
      {
        if (fileAccess.AccessLevel <= FileAccessLevel.Read)
        {
          throw new InvalidOperationException("Not enough access level for this file.");
        }

        fileData = await Resources.CreateFileData(
          transaction,
          fileAccess.UnlockedFile,
          fileData,
          me
        );

        await Internal_BroadcastFileActivity(
          transaction,
          me,
          fileAccess.UnlockedFile,
        fileAccess.FileAccess,
          new NotificationData.File.FileUpdate()
          {
            FileId = fileAccess.UnlockedFile.File.Id,
            FileDataId = fileData.Id
          }
        );
      }

      await Resources.CreateFileLog(
        transaction,
        fileAccess.UnlockedFile.File,
        me,
        request.ForWriting ? FileLogType.Update : FileLogType.Read,
        fileData
      );

      ResourceManager.FileResourceStream stream = await Resources.CreateFileStream(
        transaction,
        fileAccess.UnlockedFile,
        fileData,
        request.ForWriting
      );

      return new() { StreamId = stream.Id };
    };
}
