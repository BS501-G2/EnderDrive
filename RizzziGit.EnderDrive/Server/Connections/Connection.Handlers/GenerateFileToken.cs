using System.Linq;
using System.Security.Cryptography;
using MongoDB.Bson;
using RizzziGit.Commons.Memory;
using RizzziGit.EnderDrive.Server.Resources;

namespace RizzziGit.EnderDrive.Server.Connections;

public sealed partial class Connection
{
  private sealed record GenerateFileTokenRequest : BaseFileRequest
  {
    public required ObjectId FileDataId;
  }

  private sealed record GenerateFileTokenResponse
  {
    public required ObjectId FileTokenId;
  }

  private FileRequestHandler<
    GenerateFileTokenRequest,
    GenerateFileTokenResponse
  > GenerateFileToken =>
    async (transaction, request, userAuthentication, me, myAdminAccess, fileAccess) =>
    {
      Resource<FileData> fileData = await Internal_EnsureFirst<FileData>(
        transaction,
        (query) =>
          query.Where(
            (fileData) => fileData.Id == request.FileDataId && fileData.FileId == request.FileId
          )
      );

      ObjectId fileTokenId = ObjectId.Parse(
        CompositeBuffer
          .From(
            new Rfc2898DeriveBytes(
              CompositeBuffer
                .From($"{fileAccess.UnlockedFile.File.Id}{fileData.Id}{me.Id}")
                .ToByteArray(),
              new byte[16],
              1,
              HashAlgorithmName.SHA256
            ).GetBytes(12)
          )
          .ToHexString()
      );

      ConnectionContext context = GetContext();
      context.FileTokens.TryAdd(fileTokenId, new(fileAccess.UnlockedFile, fileData));

      return new() { FileTokenId = fileTokenId };
    };
}
