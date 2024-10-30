using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace RizzziGit.EnderDrive.Server.Services;

using System.Runtime.ExceptionServices;
using Commons.Net.WebConnection;
using Commons.Services;
using Commons.Utilities;
using RizzziGit.Commons.Memory;
using RizzziGit.EnderDrive.Utilities;

public sealed class ConnectionContext
{
    public required WebConnection Internal;
}

public enum ServerRequestCode : byte
{
    Echo
}

public enum ClientRequestCode : byte
{
    Ping
}

public sealed class Connection(ConnectionManager manager, ulong connectionId, WebSocket webSocket)
    : Service<ConnectionContext>($"Connection #{connectionId}")
{
    public ConnectionManager Manager => manager;
    public ulong ConnectionId => connectionId;

    protected override async Task<ConnectionContext> OnStart(
        CancellationToken startupCancellationToken,
        CancellationToken serviceCancellationToken
    )
    {
        WebConnection connection =
            new(
                
                webSocket,
                new() { Name = $"Connection #{ConnectionId}", Logger = ((IService)manager).Logger }
            );

        await StartServices([connection], startupCancellationToken);

        return new() { Internal = connection };
    }

    protected override async Task OnRun(
        ConnectionContext context,
        CancellationToken serviceCancellationToken
    )
    {
        await await Task.WhenAny(
            WatchService(context.Internal, serviceCancellationToken),
            RunWorker(context.Internal, serviceCancellationToken)
        );
    }

    private static async Task RunWorker(WebConnection connection, CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            WebConnectionRequest? request = await connection.ReceiveRequest(cancellationToken);
            if (request == null)
            {
                break;
            }

            using CancellationTokenSource source = cancellationToken.Link(
                request.CancellationToken
            );

            request.SendResponse(request.Request);
        }
    }

    protected override async Task OnStop(ConnectionContext context, ExceptionDispatchInfo? exception)
    {
        await StopServices(context.Internal);
    }
}
