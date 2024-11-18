using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RizzziGit.EnderDrive.Server.Connections;

using Commons.Net.WebConnection;
using Resources;

public delegate Task<ConnectionPacket<ResponseCode>> RawRequestHandler(
  ConnectionPacket<ServerSideRequestCode> request,
  CancellationToken cancellationToken
);

public sealed partial class Connection
{
  private async Task HandleRequest(
    ConnectionContext context,
    WebConnectionRequest webConnectionRequest,
    CancellationToken cancellationToken
  )
  {
    try
    {
      ConnectionPacket<ServerSideRequestCode> request =
        ConnectionPacket<ServerSideRequestCode>.Deserialize(webConnectionRequest.Data);

      ConnectionPacket<ResponseCode> response = !context.Handlers.TryGetValue(
        request.Code,
        out RawRequestHandler? handler
      )
        ? ConnectionPacket<ResponseCode>.Create(ResponseCode.NoHandlerFound, new { })
        : await handler(request, cancellationToken);

      webConnectionRequest.SendResponse(response.Serialize());
    }
    catch (Exception exception)
    {
      Error(exception);

      if (
        exception is OperationCanceledException operationCanceledException
        && operationCanceledException.CancellationToken == webConnectionRequest.CancellationToken
      )
      {
        webConnectionRequest.SendCancelResponse();
      }
      else if (exception is ConnectionResponseException connectionResponseException)
      {
        ConnectionPacket<ResponseCode> payload = ConnectionPacket<ResponseCode>.Create(
          connectionResponseException.Code,
          connectionResponseException.Data
        );

        webConnectionRequest.SendResponse(payload.Serialize());
      }
      else
      {
        webConnectionRequest.SendErrorResponse(exception.Message);
      }

      return;
    }
  }
}
