using System;
using System.Threading;
using System.Threading.Tasks;

namespace RizzziGit.EnderDrive.Server.Connections;

public sealed partial class Connection
{
  public async Task<byte[]> SendRawRequest(string name, byte[] data)
  {
    ConnectionContext context = GetContext();

    TaskCompletionSource<byte[]> source = new();

    int id;
    do
    {
      id = Random.Shared.Next();
    } while (!context.PendingRequests.TryAdd(id, source));

    await Send(
      new ConnectionPacket.Request()
      {
        Id = id,
        Name = name,
        Data = data
      },
      CancellationToken.None
    );

    return await source.Task;
  }

  public async Task<R> SendRequest<S, R>(string name, S request)
  {
    byte[] rawRequest = SerializePayload(request);
    Debug($"{request}", "->");
    byte[] rawResponse = await SendRawRequest(name, rawRequest);
    R response = DeserializePayload<R>(rawResponse);
    Debug($"{response}", "<-");

    return response;
  }
}
