using System.Threading.Tasks;

namespace RizzziGit.EnderDrive.Server.Resources;

using MongoDB.Bson;
using Services;

public sealed record FileBuffer : ResourceData
{
  public required ObjectId FileId;

  public required byte[] EncryptedBytes;
}

public sealed partial class ResourceManager
{
  public async Task<Resource<FileBuffer>> CreateBuffer(
    ResourceTransaction transaction,
    UnlockedFile file,
    byte[] buffer
  )
  {
    Resource<FileBuffer> fileBuffer = ToResource<FileBuffer>(
      transaction,
      new() { FileId = file.File.Id, EncryptedBytes = KeyManager.Encrypt(file, buffer) }
    );

    await fileBuffer.Save(transaction);
    return fileBuffer;
  }
}
