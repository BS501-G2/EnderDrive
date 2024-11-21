using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

using Commons.Collections;
using Commons.Memory;
using Commons.Utilities;
using Resources;

public sealed partial class Connection
{
  private sealed record class CreateFileRequest : BaseFileRequest
  {
    [BsonElement("name")]
    public required string Name;
  }

  private sealed record class CreateFileResponse
  {
    [BsonElement("streamId")]
    public required ObjectId StreamId;
  }

  private FileRequestHandler<CreateFileRequest, CreateFileResponse> CreateFile =>
    async (transaction, request, userAuthentication, me, _, fileAccess) =>
    {
      if (fileAccess.UnlockedFile.File.Data.Type != FileType.Folder)
      {
        throw new InvalidOperationException("Parent is not a folder.");
      }

      if (
        await Resources
          .Query<File>(
            transaction,
            (query) =>
              query
                .Where((file) => file.ParentId == fileAccess.UnlockedFile.File.Id)
                .Where((file) => file.TrashTime != null)
                .Where((file) => file.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase))
          )
          .AnyAsync(transaction.CancellationToken)
      )
      {
        throw new ConnectionResponseException(
          ResponseCode.FileNameConflict,
          new ConnectionResponseExceptionData.FileNameConflict() { Name = request.Name }
        );
      }

      UnlockedFile unlockedFile = await Resources.CreateFile(
        transaction,
        me,
        fileAccess.UnlockedFile,
        FileType.File,
        request.Name
      );

      Resource<FileContent> fileContent = await Resources.GetMainFileContent(
        transaction,
        unlockedFile.File
      );

      Resource<FileSnapshot> fileSnapshot = await Resources.CreateFileSnapshot(
        transaction,
        unlockedFile,
        fileContent,
        userAuthentication,
        null
      );

      TaskCompletionSource<ObjectId> source = new();

      RunStream(unlockedFile, fileContent, fileSnapshot, userAuthentication, source);
      await Resources.CreateFileLog(transaction, unlockedFile.File, me, FileLogType.Create);

      return new() { StreamId = await source.Task };
    };
}
