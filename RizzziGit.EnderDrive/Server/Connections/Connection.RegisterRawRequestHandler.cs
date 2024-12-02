using System;
using System.Threading;
using System.Threading.Tasks;

namespace RizzziGit.EnderDrive.Server.Connections;

public delegate Task<byte[]> RawRequestHandler(byte[] request, CancellationToken cancellationToken);

public sealed partial class Connection
{
  public void RegisterRawRequestHandler(string name, RawRequestHandler handler)
  {
    if (!GetContext().Handlers.TryAdd(name, handler))
    {
      throw new InvalidOperationException($"Handler for {name} has alaready been added.");
    }
  }
}
