using System;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;
using RizzziGit.EnderDrive.Server.Resources;

namespace RizzziGit.EnderDrive.Server.Connections;

public sealed partial class Connection
{
  private sealed record class CreateFolderRequest : BaseFileRequest
  {
    [BsonElement("name")]
    public required string Name;
  }

  private sealed record class CreateFolderResponse
  {
    [BsonElement("file")]
    public required string File;
  }

  private FileRequestHandler<CreateFolderRequest, CreateFolderResponse> CreateFolder =>
    async (transaction, request, userAuthentication, me, myAdminAccess, fileAccess) =>
    {
      ConnectionContext context = GetContext();

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

      UnlockedFile newFile = await Resources.CreateFile(
        transaction,
        me,
        fileAccess.UnlockedFile,
        FileType.Folder,
        request.Name
      );

      return new() { File = newFile.ToString() };
    };
}
