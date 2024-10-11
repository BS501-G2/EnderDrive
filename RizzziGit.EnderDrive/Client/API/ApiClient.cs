using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace RizzziGit.EnderDrive.Client.API;

using Commons.Net.HybridWebSocket2;
using Commons.Services;

public sealed class ApiClientParams
{
    public required HybridWebSocket HybridWebSocket;
}

public sealed class ApiClient(IService2 downstream)
    : Service2<ApiClientParams>("API Client", downstream)
{
    protected override async Task<ApiClientParams> OnStart(CancellationToken cancellationToken)
    {
        ClientWebSocket client = new();
        await client.ConnectAsync(new Uri("ws://localhost:8080"), cancellationToken);

        HybridWebSocket hybridWebSocket = new(client, false, "WebSocket Port", this);

        await StartServices([hybridWebSocket], cancellationToken);

        return new() { HybridWebSocket = hybridWebSocket };
    }

    protected override async Task OnRun(
        ApiClientParams context,
        CancellationToken cancellationToken
    )
    {
        await Task.Delay(-1, cancellationToken);
    }

    protected override async Task OnStop(ApiClientParams context, Exception? exception)
    {
        await StopServices(context.HybridWebSocket);
    }
}
