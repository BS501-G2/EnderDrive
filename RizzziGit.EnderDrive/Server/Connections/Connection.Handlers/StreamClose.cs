using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

using Resources;

public sealed partial class Connection
{
  private sealed record StreamCloseRequest : BaseStreamRequest { }

  private sealed record StreamCloseResponse { }

  private StreamRequestHandler<StreamCloseRequest, StreamCloseResponse> StreamClose =>
    async (request, stream, cancellationToken) =>
    {
      await stream.DisposeAsync();

      return new() { };
    };
}
