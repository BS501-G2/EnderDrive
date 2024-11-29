using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

using Resources;

public sealed partial class Connection
{
  private sealed record StreamSetLengthRequest : BaseStreamRequest
  {
    public required long Length;
  }

  private sealed record StreamSetLengthResponse { }

  private StreamRequestHandler<StreamSetLengthRequest, StreamSetLengthResponse> StreamSetLength =>
    async (request, stream, cancellationToken) =>
    {
      await stream.SetLengthAsync(request.Length, cancellationToken);

      return new() { };
    };
}
