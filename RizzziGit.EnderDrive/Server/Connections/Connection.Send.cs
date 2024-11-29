using System.Threading;
using System.Threading.Tasks;

namespace RizzziGit.EnderDrive.Server.Connections;

public sealed partial class Connection
{
  private async Task Send(ConnectionPacket packet, CancellationToken cancellationToken)
  {
    byte[] bytes = SerializePacket(packet);

    TaskCompletionSource source = new();
    await GetContext().WorkerFeed.Enqueue(new WorkerFeed.Send(source, bytes, cancellationToken), cancellationToken);

    await source.Task;
  }
}
