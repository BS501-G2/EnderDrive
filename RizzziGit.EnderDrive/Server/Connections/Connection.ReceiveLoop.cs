using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace RizzziGit.EnderDrive.Server.Connections;

using Commons.Memory;

public sealed partial class Connection
{
  private async Task RunReceiveLoop(
    ConnectionContext context,
    CancellationToken serviceCancellationToken
  )
  {
    CompositeBuffer request = [];
    int bufferSize = 1024 * 256;

    while (true)
    {
      byte[] buffer = new byte[bufferSize];
      WebSocketReceiveResult result = await webSocket.ReceiveAsync(
        buffer,
        serviceCancellationToken
      );

      request.Append(buffer, 0, result.Count);

      if (result.CloseStatus != null)
      {
        break;
      }

      if (!result.EndOfMessage || request.Length == 0)
      {
        continue;
      }

      TaskCompletionSource source = new();

      await context.WorkerFeed.Enqueue(
        new WorkerFeed.Receive(source, request.ToByteArray()),
        serviceCancellationToken
      );

      await source.Task;
      request = [];
    }
  }
}
