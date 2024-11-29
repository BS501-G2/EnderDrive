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
  [JsonProperty("createTime")]
  [BsonRepresentation(representation: BsonType.DateTime)]
  public required DateTimeOffset CreateTime;

  [JsonProperty("fileId")]
  public required ObjectId FileId;

  [JsonProperty("authorUserId")]
  public required ObjectId? AuthorUserId;

  [JsonProperty("baseFileDataId")]
  public required ObjectId? BaseFileDataId;

  [JsonIgnore]
  public required List<FileSegment> Segments;

  public long Size => Segments.Sum((e) => e.End - e.Start);
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

        CreateTime = DateTimeOffset.UtcNow,

        FileId = file.File.Id,
        AuthorUserId = authorUser?.Id,
        BaseFileDataId = baseFileData?.Id,

        Segments = baseFileData?.Data.Segments ?? [],
      }
    );

    await fileSnapshot.Save(transaction);

    return fileSnapshot;
  }

  public const int MAX_SEGMENT_SIZE = 1024 * 1024;

  private void InternalUncommitedWriteToFile(
    UnlockedFile file,
    Resource<FileData> fileData,
    long position,
    byte[] bytes
  )
  {
    List<FileSegment> leftSegments = [];
    List<FileSegment> rightSegments = [];

    foreach (FileSegment segment in fileData.Data.Segments)
    {
      long inputStart = position - segment.Start;
      long inputEnd = inputStart + bytes.Length;

      if (inputEnd <= 0)
      {
        leftSegments.Add(segment);
      }
      else if (inputStart >= segment.Size)
      {
        rightSegments.Add(segment);
      }
      else
      {
        byte[] segmentData = ReadSegment(segment, file.AesKey);

        if (inputStart > 0)
        {
          leftSegments.Add(
            CreateSegment(file.AesKey, segment.Start, segmentData[..(int)inputStart])
          );
        }

        if (inputEnd < segment.Size)
        {
          rightSegments.Add(
            CreateSegment(file.AesKey, segment.Start + inputEnd, segmentData[(int)inputEnd..])
          );
        }
      }
    }

    FileSegment centerSegment = CreateSegment(
      file.AesKey,
      leftSegments.Sum((segment) => segment.Size),
      bytes
    );

    fileData.Data.Segments = [.. leftSegments, centerSegment, .. rightSegments];
  }

  public ValueTask WriteToFile(
    ResourceTransaction transaction,
    UnlockedFile file,
    Resource<FileData> fileData,
    long position,
    byte[] bytes
  )
  {
    for (int offset = 0; offset < bytes.Length; offset += MAX_SEGMENT_SIZE)
    {
      InternalUncommitedWriteToFile(
        file,
        fileData,
        position + offset,
        bytes[offset..int.Min(MAX_SEGMENT_SIZE, bytes.Length - offset)]
      );
    }

    return fileData.Save(transaction);
  }

  public ValueTask WriteToFile(
    ResourceTransaction transaction,
    UnlockedFile file,
    Resource<FileData> fileData,
    long position,
    CompositeBuffer bytes
  )
  {
    for (int offset = 0; offset < bytes.Length; offset += MAX_SEGMENT_SIZE)
    {
      InternalUncommitedWriteToFile(
        file,
        fileData,
        position + offset,
        bytes.Slice(offset, long.Min(MAX_SEGMENT_SIZE, bytes.Length - offset)).ToByteArray()
      );
    }

    return fileData.Save(transaction);
  }

  public CompositeBuffer ReadFromFile(
    UnlockedFile file,
    Resource<FileData> fileData,
    long position,
    long length
  )
  {
    CompositeBuffer bytes = [];

    foreach (FileSegment segment in fileData.Data.Segments)
    {
      long inputStart = position - segment.Start;
      long inputEnd = inputStart + length;

      if (inputEnd <= 0)
      {
        break;
      }

      if (inputStart >= segment.Size)
      {
        continue;
      }

      byte[] segmentData = ReadSegment(segment, file.AesKey);

      bytes.Append(
        CompositeBuffer.From(
          segmentData,
          (int)long.Max(0, inputStart),
          (int)long.Min(segment.Size, inputEnd)
        )
      );
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
    if (fileData.Data.Size > length)
    {
      List<FileSegment> segments = [];

      foreach (FileSegment segment in fileData.Data.Segments)
      {
        long inputStart = 0 - segment.Start;
        long inputEnd = inputStart + length;

        if (inputEnd <= 0)
        {
          segments.Add(segment);
        }
        else
        {
          byte[] segmentData = ReadSegment(segment, file.AesKey);

          segments.Add(CreateSegment(file.AesKey, segment.Start, segmentData[..(int)inputEnd]));
          break;
        }
      }

      fileData.Data.Segments = segments;

      await fileData.Save(transaction);
    }
    else if (fileData.Data.Size < length)
    {
      fileData.Data.Segments.Add(
        CreateSegment(file.AesKey, fileData.Data.Size, new byte[length - fileData.Data.Size])
      );

      await fileData.Save(transaction);
    }
  }
}
