using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace RizzziGit.EnderDrive.Server.Resources;

using Services;

public sealed record FileSegment : ResourceData
{
  public required ObjectId FileId;
  public required ObjectId FileDataId;
  public required ObjectId FileBufferId;

  public required long DecryptedSize;
}

public sealed partial class ResourceManager
{
  private async Task<Resource<FileSegment>> CreateSegment(
    ResourceTransaction transaction,
    UnlockedFile file,
    Resource<FileData> fileData,
    byte[] buffer
  )
  {
    Resource<FileBuffer> fileBuffer = await CreateBuffer(transaction, file, buffer);

    Resource<FileSegment> fileSegment = ToResource<FileSegment>(
      transaction,
      new()
      {
        FileId = file.File.Id,
        FileDataId = fileData.Id,
        FileBufferId = fileBuffer.Id,
        DecryptedSize = buffer.Length
      }
    );

    await fileSegment.Save(transaction);
    return fileSegment;
  }

  // private async Task<Resource<FileSegment>> CloneSegment(
  //   ResourceTransaction transaction,
  //   UnlockedFile file,
  //   Resource<FileData> fileData,
  //   Resource<FileSegment> fileSegment
  // )
  // {
  //   Resource<FileSegment> newSegment = ToResource<FileSegment>(
  //     transaction,
  //     new()
  //     {
  //       FileId = file.File.Id,
  //       FileDataId = fileData.Id,
  //       FileBufferId = fileSegment.Data.FileBufferId,
  //       DecryptedSize = fileSegment.Data.DecryptedSize
  //     }
  //   );

  //   return newSegment;
  // }

  private async Task<byte[]> ReadSegment(
    ResourceTransaction transaction,
    UnlockedFile file,
    Resource<FileSegment> fileSegment
  )
  {
    Resource<FileBuffer> fileBlob = await Query<FileBuffer>(
        transaction,
        (query) =>
          query.Where(item =>
            file.File.Id == item.FileId && item.Id == fileSegment.Data.FileBufferId
          )
      )
      .FirstAsync(transaction);

    return KeyManager.Decrypt(file, fileBlob.Data.EncryptedBytes);
  }

  private async Task Delete(ResourceTransaction transaction, Resource<FileSegment> fileSegment)
  {
    if (
      await Query<FileSegment>(
          transaction,
          (query) => query.Where((item) => item.FileBufferId == fileSegment.Data.FileBufferId)
        )
        .CountAsync(transaction) <= 1
    )
    {
      Resource<FileBuffer> fileBuffer = await Query<FileBuffer>(
          transaction,
          (query) =>
            query.Where(item =>
              item.FileId == fileSegment.Data.FileId && item.Id == fileSegment.Data.FileBufferId
            )
        )
        .FirstAsync(transaction);

      await Delete(transaction, fileBuffer);
    }

    await Delete<FileSegment>(transaction, fileSegment);
  }
}
