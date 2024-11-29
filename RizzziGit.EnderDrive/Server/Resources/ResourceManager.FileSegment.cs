using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;

namespace RizzziGit.EnderDrive.Server.Resources;

public sealed record FileSegment
{
  public required long Start;
  public required long End;

  public long Size => End - Start;

  public required List<FileBlob> Blobs;
}

public sealed partial class ResourceManager
{
  public byte[] ReadSegment(FileSegment fileSegment, byte[] fileAesKey)
  {
    List<FileBlob> blobs = fileSegment.Blobs;

    List<Exception> exceptions = [];

    for (int index = 0; index < blobs.Count; index++)
    {
      FileBlob blob = blobs[index];

      try
      {
        return ReadFromBlob(blob, fileAesKey);
      }
      catch (Exception exception)
      {
        exceptions.Add(exception);

        blobs.RemoveAt(index--);
        blobs.Add(blob);
      }
    }

    string message = "Failed to read segment blob.";

    if (exceptions.Count == 0)
    {
      throw new InvalidOperationException(message);
    }
    else
    {
      throw new AggregateException(message, exceptions);
    }
  }

  public FileSegment CreateSegment(byte[] fileAesKey, long start, byte[] data)
  {
    long end = start + data.Length;

    List<FileBlob> blobs = [];

    for (int iteration = 0; iteration < blobRedundancyCount; iteration++)
    {
      blobs.Add(WriteToBlob(fileAesKey, data));
    }

    return new()
    {
      Start = start,
      End = end,

      Blobs = blobs
    };
  }
}
