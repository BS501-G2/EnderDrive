using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace RizzziGit.EnderDrive.Server.Services;

using Commons.Memory;
using Commons.Net.HybridWebSocket2;
using Commons.Services;
using Core;

public sealed class ConnectionContext
{
    public required HybridWebSocket HybridWebSocket;
}

public sealed class Connection(ConnectionManager manager, ulong connectionId, WebSocket webSocket)
    : Service2<ConnectionContext>($"Connection {connectionId}")
{
    public ConnectionManager Manager => manager;
    public ulong ConnectionId => connectionId;
    public new Func<CancellationToken, Task> Start => base.Start;

    protected override async Task<ConnectionContext> OnStart(CancellationToken cancellationToken)
    {
        HybridWebSocket hybridWebSocket = new(webSocket, true, "WebSocket Port", this);

        await StartServices([hybridWebSocket], cancellationToken);

        return new() { HybridWebSocket = hybridWebSocket };
    }

    protected override async Task OnRun(
        ConnectionContext context,
        CancellationToken cancellationToken
    )
    {
        while (true)
        {
            HybridWebSocketResult result = await context.HybridWebSocket.Receive(cancellationToken);

            switch (result)
            {
                case HybridWebSocketResult.Message message:
                    HandleMessage(message, cancellationToken);
                    break;

                case HybridWebSocketResult.Request request:
                    HandleRequest(request.RequestStream, request.ResponseStream, cancellationToken);
                    break;
            }
        }
    }

    private async void HandleMessage(
        HybridWebSocketResult.Message message,
        CancellationToken cancellationToken
    )
    {
        await Task.Delay(10, cancellationToken);
        while (true) { }
    }

    private async void HandleRequest(
        HybridWebSocket.Stream request,
        HybridWebSocket.Stream response,
        CancellationToken cancellationToken
    )
    {
        while (true)
        {
            CompositeBuffer? buffer = await request.Shift(cancellationToken);

            if (buffer == null)
            {
                await response.Finish(cancellationToken);
                break;
            }

            await response.Push(buffer, cancellationToken);
        }
    }

    protected override async Task OnStop(ConnectionContext context, Exception? exception)
    {
        await StopServices(context.HybridWebSocket);
    }
}
