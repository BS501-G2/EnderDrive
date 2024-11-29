using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

using Resources;

public sealed partial class Connection
{
  private sealed record StreamGetPositionRequest : BaseStreamRequest { }

  private sealed record StreamGetPositionResponse
  {
    public required long Position;
  }

  private StreamRequestHandler<
    StreamGetPositionRequest,
    StreamGetPositionResponse
  > StreamGetPosition =>
    async (request, stream, cancellationToken) =>
    {
      return new() { Position = await stream.GetPositionAsync(cancellationToken) };
    };
}
