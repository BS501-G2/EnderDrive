using System;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace RizzziGit.EnderDrive.Server.Resources;

using Commons.Memory;
using Newtonsoft.Json;
using Services;

public record class FileData : ResourceData
{
  [JsonIgnore]
  public required ObjectId FileId;

  [JsonIgnore]
  public required ObjectId FileContentId;

  [JsonIgnore]
  public required ObjectId FileSnapshotId;

  [JsonIgnore]
  public required ObjectId AuthorUserId;

  [JsonIgnore]
  public required ObjectId BufferId;

  [JsonIgnore]
  public required long Index;
}

public record class FileBuffer : ResourceData
{
  [JsonIgnore]
  public required ObjectId FileId;

  [JsonIgnore]
  public required ObjectId FileContentId;

  [JsonIgnore]
  public required byte[] EncryptedBuffer;
}

public sealed partial class ResourceManager
{
  public async Task<CompositeBuffer> ReadFileBlock(
    ResourceTransaction transaction,
    UnlockedFile file,
    Resource<FileSnapshot> fileSnapshot,
    long index
  )
  {
    Resource<FileData>? data = await Query<FileData>(
        transaction,
        (query) =>
          query.Where(
            (item) =>
              item.FileId == file.File.Id
              && item.Index == index
              && item.FileSnapshotId == fileSnapshot.Id
          )
      )
      .FirstOrDefaultAsync(transaction.CancellationToken);

    Resource<FileBuffer>? bufferResource =
      data != null
        ? await Query<FileBuffer>(
            transaction,
            (query) => query.Where((item) => item.Id == data.Data.BufferId)
          )
          .FirstOrDefaultAsync(transaction.CancellationToken)
        : null;

    CompositeBuffer buffer =
      bufferResource != null
        ? KeyManager.Decrypt(file, bufferResource.Data.EncryptedBuffer)
        : CompositeBuffer.Allocate(FILE_BUFFER_SIZE);

    return buffer;
  }

  public async Task WriteFileBlock(
    ResourceTransaction transaction,
    UnlockedFile file,
    Resource<FileContent> fileContent,
    Resource<FileSnapshot> fileSnapshot,
    long index,
    UnlockedUserAuthentication userAuthentication,
    CompositeBuffer bytes
  )
  {
    Resource<FileData>? data = await Query<FileData>(
        transaction,
        (query) =>
          query.Where(
            (item) =>
              item.FileId == file.File.Id
              && item.Index == index
              && item.FileSnapshotId == fileSnapshot.Id
          )
      )
      .FirstOrDefaultAsync(transaction.CancellationToken);

    bytes =
      bytes.Length < FILE_BUFFER_SIZE
        ? bytes.PadEnd(FILE_BUFFER_SIZE)
        : bytes.Slice(0, FILE_BUFFER_SIZE);

    Resource<FileBuffer> buffer = ToResource<FileBuffer>(
      transaction,
      new()
      {
        Id = ObjectId.GenerateNewId(),
        FileId = file.File.Id,
        FileContentId = fileContent.Id,
        EncryptedBuffer = KeyManager.Encrypt(file, bytes.ToByteArray()),
      }
    );

    await buffer.Save(transaction);

    if (data == null)
    {
      data = ToResource<FileData>(
        transaction,
        new()
        {
          Id = ObjectId.GenerateNewId(),

          FileId = file.File.Id,
          FileContentId = fileContent.Id,
          FileSnapshotId = fileSnapshot.Id,
          AuthorUserId = userAuthentication.UserAuthentication.Data.UserId,
          BufferId = buffer.Id,

          Index = index,
        }
      );
    }

    await data.Save(transaction);
  }

  public async Task<CompositeBuffer> ReadFile(
    ResourceTransaction transaction,
    UnlockedFile file,
    Resource<FileContent> fileContent,
    Resource<FileSnapshot> fileSnapshot,
    long position,
    long size
  )
  {
    long readStart = position;
    long readEnd = long.Min(readStart + size, fileSnapshot.Data.Size);

    long bytesRead = 0;
    long indexStart = Math.DivRem(position, FILE_BUFFER_SIZE, out long indexStartOffset);
    CompositeBuffer bytes = [];

    for (long index = indexStart; bytesRead < (readEnd - readStart); index++)
    {
      long bufferStart = indexStart == index ? indexStartOffset : 0;
      long bufferEnd = long.Clamp(
        bufferStart + long.Min(size - bytesRead, FILE_BUFFER_SIZE),
        0,
        fileSnapshot.Data.Size - readStart
      );

      if (bufferEnd - bufferStart > 0)
      {
        CompositeBuffer buffer = await ReadFileBlock(transaction, file, fileSnapshot, index);

        CompositeBuffer toRead = buffer.Slice(bufferStart, bufferEnd);

        bytes.Append(toRead);
        bytesRead += toRead.Length;
      }
    }

    return bytes;
  }

  public async Task WriteFile(
    ResourceTransaction transaction,
    UnlockedFile file,
    Resource<FileContent> fileContent,
    Resource<FileSnapshot> fileSnapshot,
    UnlockedUserAuthentication userAuthentication,
    long position,
    CompositeBuffer bytes
  )
  {
    long writeStart = position;
    long writeEnd = position + bytes.Length;

    long bytesWritten = 0;
    long indexStart = Math.DivRem(position, FILE_BUFFER_SIZE, out long indexStartOffset);

    for (long index = indexStart; bytes.Length > bytesWritten; )
    {
      long currentStart = indexStart == index ? indexStartOffset : 0;
      long currentEnd = currentStart + long.Min(bytes.Length - bytesWritten, FILE_BUFFER_SIZE);

      if (currentEnd - currentStart > 0)
      {
        CompositeBuffer current = await ReadFileBlock(transaction, file, fileSnapshot, indexStart);

        CompositeBuffer toWrite = bytes.Slice(currentStart, currentEnd);

        current.Write(currentStart, toWrite);

        await WriteFileBlock(
          transaction,
          file,
          fileContent,
          fileSnapshot,
          index,
          userAuthentication,
          current
        );

        bytesWritten += toWrite.Length;
      }
    }

    fileSnapshot.Data.Size = long.Max(position + bytes.Length, fileSnapshot.Data.Size);

    await fileSnapshot.Save(transaction);
  }
}
