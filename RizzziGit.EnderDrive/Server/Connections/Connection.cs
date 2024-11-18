using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.WebSockets;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace RizzziGit.EnderDrive.Server.Connections;

using Commons.Memory;
using Commons.Net.WebConnection;
using Commons.Services;
using Core;
using MongoDB.Bson;
using Resources;

public sealed partial class ConnectionContext
{
  public required WebConnection Internal;

  public required UnlockedUserAuthentication? CurrentUser;

  public required ConcurrentDictionary<ServerSideRequestCode, RawRequestHandler> Handlers;

  public required ulong NextFileStreamId;
  public required ConcurrentDictionary<ObjectId, ConnectionByteStream> FileStreams;
}

public sealed partial class ConnectionPacket<T>
  where T : Enum
{
  public static ConnectionPacket<T> Deserialize(CompositeBuffer bytes) =>
    new() { Code = (T)Enum.ToObject(typeof(T), bytes[0]), Data = bytes.Slice(1) };

  public static ConnectionPacket<T> Create<V>(T code, V data)
  {
    using MemoryStream stream = new();
    using BsonBinaryWriter writer = new(stream);
    BsonSerializer.Serialize(writer, data);

    return new() { Code = code, Data = stream.ToArray() };
  }

  public required T Code;
  public required CompositeBuffer Data;

  public V DeserializeData<V>()
  {
    using MemoryStream stream = new(Data.ToByteArray());
    using BsonBinaryReader reader = new(stream);

    return BsonSerializer.Deserialize<V>(reader);
  }

  public CompositeBuffer Serialize() =>
    CompositeBuffer.Concat(new byte[] { Convert.ToByte(Code) }, Data);
}

public sealed partial class Connection(
  ConnectionManager manager,
  ulong connectionId,
  WebSocket webSocket
) : Service<ConnectionContext>($"Connection #{connectionId}")
{
  public ConnectionManager Manager => manager;
  public Server Server => Manager.Server;
  public ResourceManager Resources => Server.ResourceManager;
  public UnlockedUserAuthentication? CurrentUser => GetContext().CurrentUser;

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

    ConnectionContext context =
      new()
      {
        Internal = connection,
        CurrentUser = null,
        Handlers = new(),

        NextFileStreamId = 0,
        FileStreams = new(),
      };

    RegisterHandlers(context);

    return context;
  }

  public ulong ConnectionId => connectionId;

  protected override async Task OnRun(
    ConnectionContext context,
    CancellationToken serviceCancellationToken
  )
  {
    Task[] tasks =
    [
      WatchService(context.Internal, serviceCancellationToken),
      RunWorker(context, serviceCancellationToken),
    ];

    await await Task.WhenAny(tasks);
  }

  private async Task RunWorker(ConnectionContext context, CancellationToken cancellationToken)
  {
    while (true)
    {
      cancellationToken.ThrowIfCancellationRequested();

      WebConnectionRequest? request = await context.Internal.ReceiveRequest(cancellationToken);

      if (request == null)
      {
        break;
      }

      _ = Task.Run(() => HandleRequest(context, request, cancellationToken), cancellationToken);
    }
  }

  protected override async Task OnStop(ConnectionContext context, ExceptionDispatchInfo? exception)
  {
    foreach ((_, ConnectionByteStream stream) in context.FileStreams)
    {
      await stream.Close(CancellationToken.None);
    }

    context.FileStreams.Clear();

    await StopServices(context.Internal);
  }
}
