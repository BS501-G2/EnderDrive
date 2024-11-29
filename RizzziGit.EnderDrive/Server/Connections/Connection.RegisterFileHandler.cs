using System;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

using Resources;

public sealed partial class Connection
{
  private abstract record class BaseFileRequest
  {
    [BsonElement("fileId")]
    public required ObjectId? FileId;
  }

  private delegate Task<R> FileRequestHandler<S, R>(
    ResourceTransaction transaction,
    S request,
    UnlockedUserAuthentication userAuthentication,
    Resource<User> me,
    UnlockedAdminAccess? adminAccess,
    FileAccessResult fileAccessResult
  )
    where S : BaseFileRequest;

  private void RegisterFileHandler<S, R>(
    string name,
    FileRequestHandler<S, R> requestHandler,
    FileAccessLevel? fileAccessLevel = null,
    FileType? fileType = null
  )
    where S : BaseFileRequest =>
    RegisterAuthenticatedHandler<S, R>(
      name,
      async (transaction, request, userAuthentication, me, myAdminAccess) =>
      {
        Resource<File> file = await Internal_GetFile(
          transaction,
          me,
          userAuthentication,
          request.FileId
        );

        if (fileType != null && file.Data.Type != fileType)
        {
          throw new InvalidOperationException($"Required file type: {fileType}");
        }

        FileAccessResult accessResult = await Internal_UnlockFile(
          transaction,
          file,
          me,
          userAuthentication,
          fileAccessLevel
        );

        return await requestHandler(
          transaction,
          request,
          userAuthentication,
          me,
          myAdminAccess,
          accessResult
        );
      }
    );
}
