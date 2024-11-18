using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

public sealed partial class Connection
{
  private sealed record class GetStreamPositionRequest
  {
    [BsonElement("streamId")]
    public required ObjectId StreamId;
  }

  private sealed record class GetStreamPositionResponse
  {
    [BsonElement("position")]
    public required long Position;
  }

  private RequestHandler<
    GetStreamPositionRequest,
    GetStreamPositionResponse
  > GetStreamPosition =>
    async (request, cancellationToken) =>
    {
      ConnectionContext context = GetContext();

      if (
        !context.FileStreams.TryGetValue(
          request.StreamId,
          out ConnectionByteStream? stream
        )
      )
      {
        throw new InvalidOperationException("File stream not found.");
      }

      return new() { Position = await stream.GetPosition(cancellationToken) };
    };
}
