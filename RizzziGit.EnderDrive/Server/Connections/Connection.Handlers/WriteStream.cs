using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using RizzziGit.Commons.Memory;

namespace RizzziGit.EnderDrive.Server.Connections;

public sealed partial class Connection
{
  private sealed record class WriteStreamRequest
  {
    [BsonElement("streamId")]
    public required ObjectId StreamId;

    [BsonElement("data")]
    public required byte[] Data;
  }

  private sealed record class WriteStreamResponse { }

  private RequestHandler<WriteStreamRequest, WriteStreamResponse> WriteStream =>
    async (request, cancellationToken) =>
    {
      ConnectionContext context = GetContext();

      if (!context.FileStreams.TryGetValue(request.StreamId, out ConnectionByteStream? stream))
      {
        throw new InvalidOperationException("File stream not found.");
      }

      await stream.Write(request.Data, cancellationToken);

      return new();
    };
}
