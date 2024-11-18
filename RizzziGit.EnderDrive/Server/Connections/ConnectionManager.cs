using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RizzziGit.EnderDrive.Server.Connections;

using System;
using System.Net.WebSockets;
using System.Runtime.ExceptionServices;
using Commons.Services;
using Core;
using RizzziGit.Commons.Collections;
using RizzziGit.Commons.Utilities;

public sealed class ConnectionManagerContext
{
  public required List<Connection> Connections;
  public required WaitQueue<ConnectionManagerFeed> Feed;

  public required ulong NextConnectionId;
}

public abstract record ConnectionManagerFeed
{
  private ConnectionManagerFeed() { }

  public sealed record NewConnection(
    TaskCompletionSource TaskCompletionSource,
    WebSocket WebSocket,
    CancellationToken CancellationToken
  ) : ConnectionManagerFeed();

  public sealed record Error(ExceptionDispatchInfo Exception)
    : ConnectionManagerFeed();
}

public sealed partial class ConnectionManager(Server server)
  : Service<ConnectionManagerContext>("Connections", server)
{
  public Server Server => server;

  protected override Task<ConnectionManagerContext> OnStart(
    CancellationToken startupCancellationToken,
    CancellationToken serviceCancellationToken
  )
  {
    List<Connection> connection = [];
    WaitQueue<ConnectionManagerFeed> connectionWaitQueue = new(1000);

    return Task.FromResult<ConnectionManagerContext>(
      new()
      {
        Connections = connection,
        Feed = connectionWaitQueue,
        NextConnectionId = 0,
      }
    );
  }

  protected override async Task OnRun(
    ConnectionManagerContext context,
    CancellationToken serviceCancellationToken
  )
  {
    await foreach (
      ConnectionManagerFeed feed in GetContext()
        .Feed.WithCancellation(serviceCancellationToken)
    )
    {
      switch (feed)
      {
        case ConnectionManagerFeed.NewConnection(
          TaskCompletionSource taskCompletionSource,
          WebSocket webSocket,
          CancellationToken cancellationToken
        ):
        {
          HandleConnection(
            taskCompletionSource,
            context.NextConnectionId++,
            webSocket,
            cancellationToken,
            serviceCancellationToken
          );

          break;
        }

        case ConnectionManagerFeed.Error(ExceptionDispatchInfo exception):
          exception.Throw();
          break;
      }
    }
  }

  public async Task Push(
    WebSocket webSocket,
    CancellationToken cancellationToken
  )
  {
    var context = GetContext();
    TaskCompletionSource source = new();

    await context.Feed.Enqueue(
      new ConnectionManagerFeed.NewConnection(
        source,
        webSocket,
        cancellationToken
      ),
      cancellationToken
    );

    await source.Task;
  }

  private async void HandleConnection(
    TaskCompletionSource taskCompletionSource,
    ulong connectionId,
    WebSocket webSocket,
    CancellationToken cancellationToken,
    CancellationToken serviceCancellationToken
  )
  {
    var context = GetContext();
    using CancellationTokenSource linkedCancellationTokenSource =
      serviceCancellationToken.Link(cancellationToken);

    Connection connection = new(this, connectionId, webSocket);

    await connection.Start();

    try
    {
      try
      {
        await connection.Watch(cancellationToken);
      }
      catch (Exception exception)
      {
        await context.Feed.Enqueue(
          new ConnectionManagerFeed.Error(
            ExceptionDispatchInfo.Capture(exception)
          ),
          serviceCancellationToken
        );
      }
    }
    catch (Exception exception)
    {
      taskCompletionSource.SetException(exception);
    }
    finally
    {
      taskCompletionSource.SetResult();
    }
  }
}
