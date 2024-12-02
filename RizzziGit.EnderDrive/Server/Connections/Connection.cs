using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

using Commons.Collections;
using Commons.Services;
using Core;
using Resources;

public sealed partial class ConnectionContext
{
  public required UnlockedUserAuthentication? CurrentUser;

  public required WaitQueue<Connection.WorkerFeed> WorkerFeed;
  public required ConcurrentDictionary<string, RawRequestHandler> Handlers;
  public required ConcurrentDictionary<long, TaskCompletionSource<byte[]>> PendingRequests;
}

public sealed partial class Connection(
  ConnectionManager manager,
  ulong connectionId,
  WebSocket webSocket
) : Service<ConnectionContext>($"Connection #{connectionId}", manager)
{
  public ConnectionManager Manager => manager;
  public EnderDriveServer Server => Manager.Server;
  public ResourceManager Resources => Server.Resources;
  public UnlockedUserAuthentication? CurrentUser => GetContext().CurrentUser;

  protected override async Task<ConnectionContext> OnStart(
    CancellationToken startupCancellationToken,
    CancellationToken serviceCancellationToken
  )
  {
    ConnectionContext context =
      new()
      {
        CurrentUser = null,
        Handlers = new(),
        PendingRequests = new(),
        WorkerFeed = new()
      };

    return context;
  }

  public ulong ConnectionId => connectionId;

  protected override async Task OnRun(
    ConnectionContext context,
    CancellationToken serviceCancellationToken
  )
  {
    RegisterHandlers();

    Task[] tasks =
    [
      Task.Run(() => RunReceiveLoop(context, serviceCancellationToken), CancellationToken.None),
      Task.Run(() => RunWorker(context, serviceCancellationToken), CancellationToken.None)
    ];

    await Task.WhenAny(tasks);
    WaitTasksBeforeStopping.AddRange(tasks);
  }
}
