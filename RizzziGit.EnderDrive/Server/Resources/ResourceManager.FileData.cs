using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace RizzziGit.EnderDrive.Server.Resources;

using Commons.Memory;

public record FileData : ResourceData
{
  public required long CreateTime;
  public required ObjectId FileId;
  public required ObjectId? AuthorUserId;
  public required ObjectId? BaseFileDataId;

  public required SegmentEntry[] SegmentsIds;
}

[BsonNoId]
public sealed record SegmentEntry
{
  public required ObjectId SegmentId;
  public required long Size;
}

public sealed partial class ResourceManager
{
  public async Task<Resource<FileData>> CreateFileData(
    ResourceTransaction transaction,
    UnlockedFile file,
    Resource<FileData>? baseFileData,
    Resource<User>? authorUser
  )
  {
    Resource<FileData> fileSnapshot = ToResource<FileData>(
      transaction,
      new()
      {
        Id = ObjectId.Empty,

        CreateTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),

        FileId = file.File.Id,
        AuthorUserId = authorUser?.Id,
        BaseFileDataId = baseFileData?.Id,
        SegmentsIds = []
      }
    );

    await fileSnapshot.Save(transaction);

    return fileSnapshot;
  }

  public const int MAX_SEGMENT_SIZE = 1024 * 1024;

  public async Task WriteToFile(
    ResourceTransaction transaction,
    UnlockedFile file,
    Resource<FileData> fileData,
    long position,
    byte[] bytes
  )
  {
    List<SegmentEntry> leftSegments = [];
    List<SegmentEntry> rightSegments = [];

    long segmentsRead = 0;
    foreach (SegmentEntry entry in fileData.Data.SegmentsIds)
    {
      long inputStart = position - segmentsRead;
      long inputEnd = inputStart + bytes.Length;

      if (inputEnd <= 0)
      {
        rightSegments.Add(entry);
      }
      else if (inputStart >= entry.Size)
      {
        leftSegments.Add(entry);
      }
      else
      {
        Resource<FileSegment> segment = await Query<FileSegment>(
            transaction,
            (query) =>
              query.Where(
                (fileSegment) =>
                  fileSegment.FileDataId == fileData.Id
                  && fileSegment.FileId == file.File.Id
                  && fileSegment.Id == entry.SegmentId
              )
          )
          .FirstAsync(transaction);
        byte[] segmentData = await ReadSegment(transaction, file, segment);

        if (inputStart > 0)
        {
          segment = await CreateSegment(
            transaction,
            file,
            fileData,
            segmentData[..(int)inputStart]
          );

          leftSegments.Add(new() { SegmentId = segment.Id, Size = segment.Data.DecryptedSize });
        }

        if (inputEnd < segment.Data.DecryptedSize)
        {
          segment = await CreateSegment(transaction, file, fileData, segmentData[(int)inputEnd..]);

          rightSegments.Add(new() { SegmentId = segment.Id, Size = segment.Data.DecryptedSize });
        }
      }

      segmentsRead += entry.Size;
    }

    Resource<FileSegment> centerSegment = await CreateSegment(transaction, file, fileData, bytes);

    fileData.Data.SegmentsIds =
    [
      .. leftSegments,
      new() { SegmentId = centerSegment.Id, Size = centerSegment.Data.DecryptedSize },
      .. rightSegments
    ];

    await fileData.Save(transaction);
  }

  public async Task<CompositeBuffer> ReadFromFile(
    ResourceTransaction transaction,
    UnlockedFile file,
    Resource<FileData> fileData,
    long position,
    long length
  )
  {
    CompositeBuffer bytes = [];

    long segmentsRead = 0;
    foreach (SegmentEntry entry in fileData.Data.SegmentsIds)
    {
      long inputStart = position - segmentsRead + bytes.Length;
      long inputEnd = inputStart + length - bytes.Length;

      if (inputEnd > 0 && inputStart < entry.Size)
      {
        Resource<FileSegment> segment = await Query<FileSegment>(
            transaction,
            (query) =>
              query.Where(
                (fileSegment) =>
                  fileSegment.FileDataId == fileData.Id
                  && fileSegment.FileId == file.File.Id
                  && fileSegment.Id == entry.SegmentId
              )
          )
          .FirstAsync(transaction);

        byte[] segmentData = await ReadSegment(transaction, file, segment);

        CompositeBuffer trimmed = CompositeBuffer.From(segmentData);

        bytes.Append(
          trimmed.Slice(long.Max(0, inputStart), long.Min(inputEnd, segmentData.Length))
        );
      }

      segmentsRead += entry.Size;
    }

    return bytes;
  }

  public async ValueTask TruncateFile(
    ResourceTransaction transaction,
    UnlockedFile file,
    Resource<FileData> fileData,
    long length
  )
  {
    long size = await Query<FileSegment>(
        transaction,
        (query) =>
          query.Where(
            (fileSegment) =>
              fileSegment.FileDataId == fileData.Id && fileSegment.FileId == file.File.Id
          )
      )
      .SumAsync((fileSegment) => fileSegment.Data.DecryptedSize);

    if (size > length)
    {
      List<SegmentEntry> segments = [];

      long segmentsRead = 0;
      foreach (SegmentEntry entry in fileData.Data.SegmentsIds)
      {
        long inputStart = 0 - segmentsRead;
        long inputEnd = inputStart + length;

        if (inputEnd <= 0)
        {
          segments.Add(entry);
        }
        else
        {
          Resource<FileSegment> segment = await Query<FileSegment>(
              transaction,
              (query) =>
                query.Where(
                  (segment) => segment.FileDataId == fileData.Id && segment.FileId == file.File.Id
                )
            )
            .FirstAsync(transaction);

          byte[] segmentData = await ReadSegment(transaction, file, segment);

          segment = await CreateSegment(
            transaction,
            file,
            fileData,
            segmentData[..(int)long.Min(inputEnd, segmentData.Length)]
          );
          segments.Add(new() { SegmentId = segment.Id, Size = segment.Data.DecryptedSize });
          break;
        }

        segmentsRead += entry.Size;
      }

      fileData.Data.SegmentsIds = [.. segments];

      await fileData.Save(transaction);
    }
    else if (size < length)
    {
      Resource<FileSegment> segment = await CreateSegment(
        transaction,
        file,
        fileData,
        new byte[length - size]
      );
      fileData.Data.SegmentsIds =
      [
        .. fileData.Data.SegmentsIds,
        new() { SegmentId = segment.Id, Size = segment.Data.DecryptedSize }
      ];

      await fileData.Save(transaction);
    }
  }

  public async ValueTask<long> GetFileSize(
    ResourceTransaction transaction,
    UnlockedFile file,
    Resource<FileData> fileData
  )
  {
    long size = await Query<FileSegment>(
        transaction,
        (query) =>
          query.Where(
            (fileSegment) =>
              fileSegment.FileDataId == fileData.Id && fileSegment.FileId == file.File.Id
          )
      )
      .SumAsync((fileSegment) => fileSegment.Data.DecryptedSize);

    return size;
  }

  public async Task Delete(ResourceTransaction transaction, Resource<FileData> fileData) {
    
  }
}
