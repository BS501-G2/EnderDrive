using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace RizzziGit.EnderDrive.Server.Connections;

public sealed partial class Connection
{
  public abstract record WorkerFeed
  {
    private WorkerFeed() { }

    public sealed record Receive(TaskCompletionSource Source, byte[] Bytes) : WorkerFeed;

    public sealed record Send(
      TaskCompletionSource Source,
      byte[] Bytes,
      CancellationToken CancellationToken
    ) : WorkerFeed;

    public sealed record Close(TaskCompletionSource Source) : WorkerFeed;
  }

  private async Task RunWorker(
    ConnectionContext context,
    CancellationToken serviceCancellationToken
  )
  {
    await foreach (
      WorkerFeed entry in context.WorkerFeed.WithCancellation(serviceCancellationToken)
    )
    {
      switch (entry)
      {
        case WorkerFeed.Receive(TaskCompletionSource source, byte[] bytes):
        {
          _ = Task.Run(
            async () =>
            {
              try
              {
                await HandlePacket(context, DeserializePacket(bytes), serviceCancellationToken);
              }
              catch (Exception exception)
              {
                Error(exception);
              }
            },
            CancellationToken.None
          );

          source.SetResult();
          break;
        }

        case WorkerFeed.Send(
          TaskCompletionSource source,
          byte[] bytes,
          CancellationToken CancellationToken
        ):
        {
          try
          {
            await webSocket.SendAsync(
              bytes,
              WebSocketMessageType.Binary,
              true,
              serviceCancellationToken
            );
            source.SetResult();
          }
          catch (Exception exception)
          {
            source.SetException(exception);
            throw;
          }

          break;
        }

        case WorkerFeed.Close(TaskCompletionSource source):
        {
          source.SetResult();
          return;
        }
      }
    }
  }
}
