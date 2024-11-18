using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using RizzziGit.Commons.Memory;

namespace RizzziGit.EnderDrive.Server.Connections;

public sealed partial class Connection
{
  private sealed record class ReadStreamRequest
  {
    [BsonElement("streamId")]
    public required ObjectId StreamId;

    [BsonElement("length")]
    public required long Length;
  }

  private sealed record class ReadStreamResponse
  {
    [BsonElement("data")]
    public required byte[] Data;
  }

  private RequestHandler<ReadStreamRequest, ReadStreamResponse> ReadStream =>
    async (request, cancellationToken) =>
    {
      ConnectionContext context = GetContext();

      if (!context.FileStreams.TryGetValue(request.StreamId, out ConnectionByteStream? stream))
      {
        throw new InvalidOperationException("File stream not found.");
      }

      CompositeBuffer data = await stream.Read(request.Length, cancellationToken);

      return new() { Data = data.ToByteArray() };
    };
}
