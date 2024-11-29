using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

using Resources;

public sealed partial class Connection
{
  private sealed record StreamSetPositionRequest : BaseStreamRequest
  {
    public required long Position;
  }

  private sealed record StreamSetPositionResponse { }

  private StreamRequestHandler<
    StreamSetPositionRequest,
    StreamSetPositionResponse
  > StreamSetPosition =>
    async (request, stream, cancellationToken) =>
    {
      await stream.SeekAsync(request.Position, System.IO.SeekOrigin.Begin, cancellationToken);

      return new() { };
    };
}
