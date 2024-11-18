using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace RizzziGit.EnderDrive.Server.Resources;

using Commons.Memory;
using Commons.Utilities;
using Utilities;

public sealed partial class ResourceManager
{
  public async Task<Stream> CreateReadStream(
    ResourceTransaction transaction,
    UnlockedFile file,
    Resource<FileContent> content,
    Resource<FileSnapshot> snapshot
  )
  {
    FileStream stream = new(this, transaction, file, content, snapshot, null);

    return stream;
  }

  public async Task<Stream> CreateWriteStream(
    ResourceTransaction transaction,
    UnlockedFile file,
    Resource<FileContent> content,
    Resource<FileSnapshot> snapshot,
    UnlockedUserAuthentication userAuthentication,
    bool createNewSnapshot
  )
  {
    FileStream stream =
      new(
        this,
        transaction,
        file,
        content,
        createNewSnapshot
          ? await CreateFileSnapshot(transaction, file, content, userAuthentication, snapshot)
          : snapshot,
        userAuthentication
      );

    return stream;
  }

  internal sealed class FileStream(
    ResourceManager manager,
    ResourceTransaction transaction,
    UnlockedFile file,
    Resource<FileContent> content,
    Resource<FileSnapshot> snapshot,
    UnlockedUserAuthentication? userAuthentication
  ) : Stream
  {
    public readonly UnlockedFile File = file;
    public readonly Resource<FileContent> FileContent = content;
    public readonly Resource<FileSnapshot> FileSnapshot = snapshot;

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => userAuthentication != null;

    public override long Length => FileSnapshot.Data.Size;

    private long position = 0;

    public override long Position
    {
      get => position;
      set
      {
        if (value >= 0 && value <= FileSnapshot.Data.Size)
        {
          position = value;
        }
        else
        {
          throw new ArgumentOutOfRangeException(
            nameof(value),
            value,
            $"Must must range from 0 to {FileSnapshot.Data.Size}"
          );
        }
      }
    }

    public override void Flush() { }

    public override async ValueTask<int> ReadAsync(
      Memory<byte> buffer,
      CancellationToken cancellationToken = default
    ) =>
      await Task.Run(
        async () =>
        {
          CompositeBuffer fileRead = await manager.ReadFile(
            transaction,
            File,
            FileContent,
            FileSnapshot,
            Position,
            long.Min(buffer.Span.Length, Length - Position)
          );

          fileRead.ToByteArray().CopyTo(buffer.Span);
          position += fileRead.Length;
          return (int)fileRead.Length;
        },
        default
      );

    public override async ValueTask WriteAsync(
      ReadOnlyMemory<byte> buffer,
      CancellationToken cancellationToken = default
    ) =>
      await Task.Run(
        async () =>
        {
          if (userAuthentication is null)
          {
            throw new InvalidOperationException("Not authenticated.");
          }

          await manager.WriteFile(
            transaction,
            File,
            FileContent,
            FileSnapshot,
            userAuthentication,
            position,
            buffer.ToArray()
          );

          position += buffer.Length;
        },
        default
      );

    public override void SetLength(long value) { }

    public override long Seek(long offset, SeekOrigin origin) =>
      origin switch
      {
        SeekOrigin.Begin => Position = offset,
        SeekOrigin.Current => Position += offset,
        SeekOrigin.End => Position = Length - offset,
        _ => throw new InvalidOperationException("Invalid offset."),
      };

    public override void Write(byte[] buffer, int offset, int count) =>
      WriteAsync(buffer, 0, 0, CancellationToken.None).GetAwaiter().GetResult();

    public override async Task WriteAsync(
      byte[] buffer,
      int offset,
      int count,
      CancellationToken cancellationToken
    ) => await WriteAsync(buffer.AsMemory(offset, count), cancellationToken);

    public override int Read(byte[] buffer, int offset, int count) =>
      ReadAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();

    public override async Task<int> ReadAsync(
      byte[] buffer,
      int offset,
      int count,
      CancellationToken cancellationToken
    ) => await ReadAsync(buffer.AsMemory(offset, count), cancellationToken);
  }
}
