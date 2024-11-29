using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

using Resources;

public sealed partial class Connection
{
  private sealed record StreamGetLengthRequest : BaseStreamRequest { }

  private sealed record StreamGetLengthResponse
  {
    public required long Length;
  }

  private StreamRequestHandler<StreamGetLengthRequest, StreamGetLengthResponse> StreamGetLength =>
    async (request, stream, cancellationToken) =>
    {
      return new() { Length = await stream.GetLengthAsync(cancellationToken) };
    };
}
