using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RizzziGit.EnderDrive.Server.Services;

using System;
using System.Net.WebSockets;
using Commons.Net.HybridWebSocket;
using Commons.Services;
using Core;
using RizzziGit.Commons.Collections;

public sealed record ConnectionWaitQueueEntry(
    TaskCompletionSource<Connection> Source,
    WebSocket WebSocket,
    CancellationToken CancellationToken
);

public sealed class ConnectionManagerParams
{
    public required List<Connection> Connections;
    public required WaitQueue<ConnectionWaitQueueEntry> ConnectionWaitQueue;
}

public sealed partial class ConnectionManager(Server server)
    : Service2<ConnectionManagerParams>("Connections", server)
{
    protected override Task<ConnectionManagerParams> OnStart(CancellationToken cancellationToken)
    {
        List<Connection> connection = [];
        WaitQueue<ConnectionWaitQueueEntry> connectionWaitQueue = new(1000);

        return Task.FromResult<ConnectionManagerParams>(
            new() { Connections = connection, ConnectionWaitQueue = connectionWaitQueue }
        );
    }

    public async Task<Connection> Connect(WebSocket webSocket, CancellationToken cancellationToken)
    {
        TaskCompletionSource<Connection> source = new();

        await Context.ConnectionWaitQueue.Enqueue(
            new(source, webSocket, cancellationToken),
            cancellationToken
        );

        return await source.Task;
    }

    protected override async Task OnRun(
        ConnectionManagerParams data,
        CancellationToken serviceCancellationToken
    )
    {
        await foreach (
            var (
                source,
                webSocket,
                cancellationToken
            ) in Context.ConnectionWaitQueue.WithCancellation(serviceCancellationToken)
        )
            HandleConnection(source, webSocket, cancellationToken, serviceCancellationToken);
    }

    private ulong nextConnectionId = 0;

    private async void HandleConnection(
        TaskCompletionSource<Connection> source,
        WebSocket webSocket,
        CancellationToken cancellationToken,
        CancellationToken serviceCancellationToken
    )
    {
        using CancellationTokenSource linkedCancellationToken =
            CancellationTokenSource.CreateLinkedTokenSource(
                serviceCancellationToken,
                cancellationToken
            );

        Connection connection;
        try
        {
            connection = new(this, ++nextConnectionId, webSocket);

            await connection.Start(linkedCancellationToken.Token);
            source.SetResult(connection);
        }
        catch (Exception exception)
        {
            source.SetException(exception);
            return;
        }

        lock (Context.Connections)
        {
            Context.Connections.Add(connection);
        }

        try
        {
            await WatchService(connection, serviceCancellationToken);
        }
        catch { }
        finally
        {
            lock (Context.Connections)
            {
                Context.Connections.Remove(connection);
            }

            await connection.Stop();
        }
    }
}
