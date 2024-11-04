using System;
using System.Threading;
using System.Threading.Tasks;

namespace RizzziGit.EnderDrive.Server.Services;

using Commons.Net.WebConnection;
using Resources;

public sealed partial class Connection
{
    private async Task HandleRequest(
        ConnectionContext context,
        WebConnectionRequest webConnectionRequest,
        CancellationToken cancellationToken
    )
    {
        await Resources.Transact(
            async (transaction) =>
            {
                try
                {
                    ConnectionPacket<ServerSideRequestCode> request =
                        ConnectionPacket<ServerSideRequestCode>.Deserialize(
                            webConnectionRequest.Data
                        );

                    ConnectionPacket<ResponseCode> response = !context.Handlers.TryGetValue(
                        request.Code,
                        out RawRequestHandler? handler
                    )
                        ? ConnectionPacket<ResponseCode>.Create(
                            ResponseCode.NoHandlerFound,
                            new { }
                        )
                        : await handler(transaction, request);

                    webConnectionRequest.SendResponse(response.Serialize());
                }
                catch (Exception exception)
                {
                    if (
                        exception is OperationCanceledException operationCanceledException
                        && operationCanceledException.CancellationToken
                            == webConnectionRequest.CancellationToken
                    )
                    {
                        webConnectionRequest.SendCancelResponse();
                    }
                    else
                    {
                        webConnectionRequest.SendErrorResponse(
                            exception is ConnectionResponseException webConnectionResponseException
                                ? webConnectionResponseException.Data
                                : exception.Message
                        );
                    }

                    return;
                }
            },
            cancellationToken
        );
    }
}
