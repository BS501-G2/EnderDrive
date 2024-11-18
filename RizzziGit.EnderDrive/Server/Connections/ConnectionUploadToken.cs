using System;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace RizzziGit.EnderDrive.Server.Connections;

using Commons.Collections;
using Commons.Memory;

public sealed record ConnectionByteStream(
  ObjectId TokenId,
  WaitQueue<ConnectionByteStream.Feed> Queue
)
{
  public abstract record Feed(CancellationToken CancellationToken)
  {
    public sealed record Read(
      TaskCompletionSource<CompositeBuffer> Source,
      long Count,
      CancellationToken CancellationToken
    ) : Feed(CancellationToken);

    public sealed record Write(
      TaskCompletionSource Source,
      CompositeBuffer Buffer,
      CancellationToken CancellationToken
    ) : Feed(CancellationToken);

    public sealed record SetPosition(
      TaskCompletionSource Source,
      long Offset,
      CancellationToken CancellationToken
    ) : Feed(CancellationToken);

    public sealed record GetPosition(
      TaskCompletionSource<long> Source,
      CancellationToken CancellationToken
    ) : Feed(CancellationToken);

    public sealed record GetLength(
      TaskCompletionSource<long> Source,
      CancellationToken CancellationToken
    ) : Feed(CancellationToken);

    public sealed record Close(TaskCompletionSource Source, CancellationToken CancellationToken)
      : Feed(CancellationToken);
  }

  public async Task<CompositeBuffer> Read(long length, CancellationToken cancellationToken)
  {
    TaskCompletionSource<CompositeBuffer> source = new();

    await Queue.Enqueue(new Feed.Read(source, length, cancellationToken), cancellationToken);
    return await source.Task;
  }

  public async Task Write(CompositeBuffer bytes, CancellationToken cancellationToken)
  {
    TaskCompletionSource source = new();

    await Queue.Enqueue(new Feed.Write(source, bytes, cancellationToken), cancellationToken);
    await source.Task;
  }

  public async Task SetPosition(long offset, CancellationToken cancellationToken)
  {
    TaskCompletionSource source = new();

    await Queue.Enqueue(new Feed.SetPosition(source, offset, cancellationToken), cancellationToken);
    await source.Task;
  }

  public async Task<long> GetPosition(CancellationToken cancellationToken)
  {
    TaskCompletionSource<long> source = new();

    await Queue.Enqueue(new Feed.GetPosition(source, cancellationToken), cancellationToken);
    return await source.Task;
  }

  public async Task<long> GetLength(CancellationToken cancellationToken)
  {
    TaskCompletionSource<long> source = new();

    await Queue.Enqueue(new Feed.GetLength(source, cancellationToken), cancellationToken);
    return await source.Task;
  }

  public async Task Close(CancellationToken cancellationToken)
  {
    TaskCompletionSource source = new();

    await Queue.Enqueue(new Feed.Close(source, cancellationToken), cancellationToken);
  }
}
