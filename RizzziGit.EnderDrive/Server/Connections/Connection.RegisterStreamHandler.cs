using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

using Resources;

public sealed partial class Connection
{
  private abstract record BaseStreamRequest
  {
    [BsonElement("streamId")]
    public required ObjectId StreamId;
  }

  private delegate Task<R> StreamRequestHandler<S, R>(
    S request,
    ResourceManager.FileResourceStream stream,
    CancellationToken cancellationToken
  )
    where S : BaseStreamRequest;

  private void RegisterStreamHandler<S, R>(string name, StreamRequestHandler<S, R> streamHandler)
    where S : BaseStreamRequest =>
    RegisterHandler<S, R>(
      name,
      async (request, cancellationToken) =>
      {
        if (
          !Resources.TryGetActiveFileStream(
            request.StreamId,
            out ResourceManager.FileResourceStream? stream
          )
        )
        {
          throw new NotFoundException() { ResourceName = "Stream" };
        }

        return await streamHandler(request, stream, cancellationToken);
      }
    );
}
