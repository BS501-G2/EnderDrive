using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace RizzziGit.EnderDrive.Server.Resources;

using Commons.Collections;
using Commons.Memory;

public sealed partial class ResourceManager
{
  public abstract record StreamFeed(CancellationToken CancellationToken)
  {
    public sealed record Read(
      TaskCompletionSource<CompositeBuffer> Source,
      long Length,
      CancellationToken CancellationToken
    ) : StreamFeed(CancellationToken);

    public sealed record Write(
      TaskCompletionSource Source,
      CompositeBuffer Bytes,
      CancellationToken CancellationToken
    ) : StreamFeed(CancellationToken);

    public sealed record SetPosition(
      TaskCompletionSource<long> Source,
      long Position,
      CancellationToken CancellationToken
    ) : StreamFeed(CancellationToken);

    public sealed record GetPosition(
      TaskCompletionSource<long> Source,
      CancellationToken CancellationToken
    ) : StreamFeed(CancellationToken);

    public sealed record GetLength(
      TaskCompletionSource<long> Source,
      CancellationToken CancellationToken
    ) : StreamFeed(CancellationToken);

    public sealed record SetLength(
      TaskCompletionSource Source,
      long Length,
      CancellationToken CancellationToken
    ) : StreamFeed(CancellationToken);

    public sealed record Close(TaskCompletionSource Source, CancellationToken CancellationToken)
      : StreamFeed(CancellationToken);
  }

  public sealed class FileResourceStream(
    WaitQueue<StreamFeed> feed,
    Func<ObjectId> getStreamId,
    Task task,
    bool canWrite
  ) : Stream
  {
    public Task StreamLoopTask => task;
    public ObjectId Id => getStreamId();

    public override bool CanRead => true;

    public override bool CanSeek => true;

    public override bool CanWrite => canWrite;

    public override long Length => GetLengthAsync().GetAwaiter().GetResult();

    public override long Position
    {
      get => GetPositionAsync().GetAwaiter().GetResult();
      set => Seek(value, SeekOrigin.Begin);
    }

    public override void Flush() { }

    public override int Read(byte[] buffer, int offset, int count) =>
      ReadAsync(buffer, offset, count).GetAwaiter().GetResult();

    public override long Seek(long offset, SeekOrigin origin) =>
      SeekAsync(offset, origin).GetAwaiter().GetResult();

    public override void SetLength(long value) => SetLengthAsync(value).GetAwaiter().GetResult();

    public override void Write(byte[] buffer, int offset, int count) =>
      WriteAsync(buffer, offset, count).GetAwaiter().GetResult();

    public override void Close() => CloseAsync().GetAwaiter().GetResult();

    public async Task<CompositeBuffer> ReadAsync(long length, CancellationToken cancellationToken)
    {
      TaskCompletionSource<CompositeBuffer> source = new();

      await feed.Enqueue(new StreamFeed.Read(source, length, cancellationToken), cancellationToken);

      return await source.Task;
    }

    public override async Task<int> ReadAsync(
      byte[] buffer,
      int offset,
      int count,
      CancellationToken cancellationToken
    )
    {
      CompositeBuffer result = await ReadAsync(count, cancellationToken);

      result.Read(0, buffer, offset, count);

      return (int)result.Length;
    }

    public async Task WriteAsync(CompositeBuffer bytes, CancellationToken cancellationToken)
    {
      TaskCompletionSource source = new();

      await feed.Enqueue(new StreamFeed.Write(source, bytes, cancellationToken), cancellationToken);
      await source.Task;
    }

    public override Task WriteAsync(
      byte[] buffer,
      int offset,
      int count,
      CancellationToken cancellationToken
    ) => WriteAsync(CompositeBuffer.From(buffer, offset, count), cancellationToken);

    public async Task<long> SeekAsync(
      long newPosition,
      SeekOrigin origin,
      CancellationToken cancellationToken = default
    )
    {
      if (origin == SeekOrigin.End)
      {
        return await SeekAsync(Length - newPosition, SeekOrigin.Begin, cancellationToken);
      }
      else if (origin == SeekOrigin.Current)
      {
        return await SeekAsync(Position + newPosition, SeekOrigin.Begin, cancellationToken);
      }
      else if (origin == SeekOrigin.Begin)
      {
        TaskCompletionSource<long> source = new();

        await feed.Enqueue(
          new StreamFeed.SetPosition(source, newPosition, cancellationToken),
          cancellationToken
        );

        return await source.Task;
      }
      else
      {
        throw new ArgumentException("Invalid seek origin.", nameof(origin));
      }
    }

    public async Task<long> GetPositionAsync(CancellationToken cancellationToken = default)
    {
      TaskCompletionSource<long> source = new();

      await feed.Enqueue(new StreamFeed.GetPosition(source, cancellationToken), cancellationToken);

      return await source.Task;
    }

    public async Task SetLengthAsync(long value, CancellationToken cancellationToken = default)
    {
      TaskCompletionSource source = new();

      await feed.Enqueue(
        new StreamFeed.SetLength(source, value, cancellationToken),
        cancellationToken
      );

      await source.Task;
    }

    public async Task<long> GetLengthAsync(CancellationToken cancellationToken = default)
    {
      TaskCompletionSource<long> source = new();

      await feed.Enqueue(new StreamFeed.GetLength(source, cancellationToken), cancellationToken);

      return await source.Task;
    }

    public async Task CloseAsync(CancellationToken cancellationToken = default)
    {
      TaskCompletionSource source = new();

      await feed.Enqueue(new StreamFeed.Close(source, cancellationToken), cancellationToken);

      await source.Task;
    }
  }

  private async Task RunFileStream(
    UnlockedFile file,
    Resource<FileData> fileData,
    WaitQueue<StreamFeed> stream,
    bool allowWrite = false
  )
  {
    long position = 0;

    while (true)
    {
      using CancellationTokenSource externalCancellationTokenSource = new(TimeSpan.FromMinutes(5));

      StreamFeed feed = await stream.Dequeue(externalCancellationTokenSource.Token);

      bool isCanceled(CancellationToken cancellationToken) =>
        feed.CancellationToken.IsCancellationRequested;

      switch (feed)
      {
        case StreamFeed.Read(
          TaskCompletionSource<CompositeBuffer> source,
          long length,
          CancellationToken cancellationToken
        ):
        {
          if (isCanceled(cancellationToken))
          {
            source.SetCanceled(cancellationToken);
            break;
          }

          try
          {
            CompositeBuffer result = ReadFromFile(file, fileData, position, length);

            source.SetResult(result);
            position += result.Length;
          }
          catch (Exception exception)
          {
            source.SetException(exception);
          }

          break;
        }

        case StreamFeed.Write(
          TaskCompletionSource source,
          CompositeBuffer bytes,
          CancellationToken cancellationToken
        ):
        {
          if (isCanceled(cancellationToken))
          {
            source.SetCanceled(cancellationToken);
            break;
          }

          if (!allowWrite)
          {
            source.SetException(
              ExceptionDispatchInfo.SetCurrentStackTrace(new NotSupportedException())
            );
            break;
          }

          try
          {
            await Transact(
              async (transaction) =>
              {
                await WriteToFile(transaction, file, fileData, position, bytes);
              },
              cancellationToken
            );
            position += bytes.Length;
          }
          catch (Exception exception)
          {
            source.SetException(exception);
          }
          break;
        }

        case StreamFeed.SetPosition(
          TaskCompletionSource<long> source,
          long newPosition,
          CancellationToken cancellationToken
        ):
        {
          if (isCanceled(cancellationToken))
          {
            source.SetCanceled(cancellationToken);
            break;
          }

          try
          {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(newPosition, fileData.Data.Size);

            source.SetResult(position = newPosition);
          }
          catch (Exception exception)
          {
            source.SetException(exception);
          }

          break;
        }

        case StreamFeed.GetPosition(
          TaskCompletionSource<long> source,
          CancellationToken cancellationToken
        ):
        {
          if (isCanceled(cancellationToken))
          {
            source.SetCanceled(cancellationToken);
            break;
          }

          source.SetResult(position);
          break;
        }

        case StreamFeed.GetLength(
          TaskCompletionSource<long> source,
          CancellationToken cancellationToken
        ):
        {
          if (isCanceled(cancellationToken))
          {
            source.SetCanceled(cancellationToken);
            break;
          }

          source.SetResult(fileData.Data.Size);
          break;
        }

        case StreamFeed.SetLength(
          TaskCompletionSource source,
          long length,
          CancellationToken cancellationToken
        ):
        {
          if (isCanceled(cancellationToken))
          {
            source.SetCanceled(cancellationToken);
            break;
          }

          try
          {
            await Transact(
              async (transaction) =>
              {
                await TruncateFile(transaction, file, fileData, length);
              },
              cancellationToken
            );

            position = long.Min(position, length);
            source.SetResult();
          }
          catch (Exception exception)
          {
            source.SetException(exception);
          }

          break;
        }

        case StreamFeed.Close(TaskCompletionSource source, CancellationToken cancellationToken):
        {
          if (isCanceled(cancellationToken))
          {
            source.SetCanceled(cancellationToken);
            break;
          }

          return;
        }
      }
    }
  }

  public FileResourceStream CreateFileStream(
    UnlockedFile file,
    Resource<FileData> fileData,
    bool allowWrite = false
  )
  {
    ResourceManagerContext context = GetContext();
    WaitQueue<StreamFeed> feed = new(0);

    lock (context.ActiveFileStreams)
    {
      Task task = Task.Run(async () =>
      {
        using (feed)
        {
          try
          {
            await RunFileStream(file, fileData, feed, allowWrite);
          }
          catch { }
        }
      });

      ObjectId streamId = ObjectId.GenerateNewId();
      FileResourceStream stream = new(feed, () => streamId, task, allowWrite);

      while (!context.ActiveFileStreams.TryAdd(streamId, stream))
      {
        streamId = ObjectId.GenerateNewId();
      }

      return stream;
    }
  }

  public bool TryGetActiveFileStream(
    ObjectId streamId,
    [NotNullWhen(true)] out FileResourceStream? fileResourceStream
  ) => GetContext().ActiveFileStreams.TryGetValue(streamId, out fileResourceStream);
}
