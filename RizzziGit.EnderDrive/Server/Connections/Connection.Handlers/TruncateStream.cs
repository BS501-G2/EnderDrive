using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

public sealed partial class Connection
{
  private sealed record class TruncateStreamRequest
  {
    [BsonElement("streamId")]
    public required ObjectId StreamId;

    [BsonElement("length")]
    public required long Length;
  }

  private sealed record class TruncateStreamResponse
  {
    [BsonElement("newFileSnapshotId")]
    public required ObjectId? NewFileSnapshotId;
  }

  private RequestHandler<TruncateStreamRequest, TruncateStreamResponse> TruncateStream =>
    async (request, cancellationToken) =>
    {
      ConnectionContext context = GetContext();

      if (!context.FileStreams.TryGetValue(request.StreamId, out ConnectionByteStream? stream))
      {
        throw new InvalidOperationException("File stream not found.");
      }
      return new() { NewFileSnapshotId = await stream.Truncate(request.Length, cancellationToken) };
    };
}
