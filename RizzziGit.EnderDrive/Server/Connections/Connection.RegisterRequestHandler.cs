using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace RizzziGit.EnderDrive.Server.Connections;

public sealed partial class Connection
{
  private static T DeserializePayload<T>(byte[] bytes)
  {
    using MemoryStream stream = new(bytes);
    using BsonBinaryReader reader = new(stream);

    return BsonSerializer.Deserialize<T>(reader);
  }

  private static byte[] SerializePayload<T>(T obj)
  {
    using MemoryStream stream = new();
    using BsonBinaryWriter writer = new(stream);

    BsonSerializer.Serialize(writer, obj);
    return stream.ToArray();
  }

  private delegate Task<R> RequestHandler<S, R>(S request, CancellationToken cancellationToken);

  private void RegisterRequestHandler<S, R>(string name, RequestHandler<S, R> handler) =>
    RegisterRawRequestHandler(
      name,
      async (rawRequest, cancellationToken) =>
      {
        S request = DeserializePayload<S>(rawRequest);
        // Debug($"{request}", "<-");
        R response = await handler(request, cancellationToken);
        // Debug($"{response}", "->");

        return SerializePayload(response);
      }
    );
}
