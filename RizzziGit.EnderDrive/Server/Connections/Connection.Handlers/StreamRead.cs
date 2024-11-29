using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

using Commons.Memory;
using Resources;

public sealed partial class Connection
{
  private sealed record StreamReadRequest : BaseStreamRequest
  {
    public required long Length;
  }

  private sealed record StreamReadResponse
  {
    public required byte[] Data;
  }

  private StreamRequestHandler<StreamReadRequest, StreamReadResponse> StreamRead =>
    async (request, stream, cancellationToken) =>
    {
      CompositeBuffer data = await stream.ReadAsync(request.Length, cancellationToken);

      return new() { Data = data.ToByteArray() };
    };
}
