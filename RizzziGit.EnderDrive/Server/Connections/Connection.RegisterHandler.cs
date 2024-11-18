using System;
using System.Threading;
using System.Threading.Tasks;

namespace RizzziGit.EnderDrive.Server.Connections;

public sealed partial class Connection
{
  private delegate Task<R> RequestHandler<S, R>(S request, CancellationToken cancellationToken);

  private static void RegisterHandler<S, R>(
    ConnectionContext context,
    ServerSideRequestCode code,
    RequestHandler<S, R> handler
  )
  {
    if (
      !context.Handlers.TryAdd(
        code,
        async (requestBuffer, cancellationToken) =>
        {
          try
          {
            S request = requestBuffer.DeserializeData<S>();
            R response = await handler(request, cancellationToken);

            return ConnectionPacket<ResponseCode>.Create(ResponseCode.OK, response);
          }
          catch (ConnectionResponseException exception)
          {
            return ConnectionPacket<ResponseCode>.Create(exception.Code, exception.Data);
          }
        }
      )
    )
    {
      throw new InvalidOperationException("Handler is already added.");
    }
  }
}
