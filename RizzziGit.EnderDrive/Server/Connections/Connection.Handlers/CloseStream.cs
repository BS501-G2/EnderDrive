using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

public sealed partial class Connection
{
  private sealed record class CloseStreamRequest
  {
    [BsonElement("streamId")]
    public required ObjectId StreamId;
  }

  private sealed record class CloseStreamResponse { }

  private RequestHandler<CloseStreamRequest, CloseStreamResponse> CloseStream =>
    async (request, cancellationToken) =>
    {
      ConnectionContext context = GetContext();

      if (!context.FileStreams.TryGetValue(request.StreamId, out ConnectionByteStream? stream))
      {
        throw new InvalidOperationException("File stream not found.");
      }

      await stream.Close(cancellationToken);

      return new() { };
    };
}
