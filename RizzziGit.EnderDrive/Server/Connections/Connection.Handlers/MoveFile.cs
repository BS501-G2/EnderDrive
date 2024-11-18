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
      Resource<File> file =
        request.NewParentId != null
          ? await Internal_GetFile(transaction, me, userAuthentication, request.NewParentId.Value)
          : await Internal_GetFile(transaction, me, userAuthentication, request.FileId);

      FileAccessResult unlockedFile = await Internal_UnlockFile(
        transaction,
        file,
        me,
        userAuthentication
      );

    //   if (
    //     await Resources
    //       .Query<File>(
    //         transaction,
    //         (query) => query.Where((file) => file.ParentId == request.FileId)
    //       )
    //       .AnyAsync(transaction.CancellationToken)
    //   )
    //   {
    //     throw new ConnectionResponseException(
    //       ResponseCode.FileNameConflict,
    //       new ConnectionResponseExceptionData.FileNameConflict() { Name = request.Name }
    //     );
    //   }

      return new() { };
    };
}
