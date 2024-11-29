using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

using Resources;

public sealed partial class Connection
{
  private sealed record StreamWriteRequest : BaseStreamRequest
  {
    public required byte[] Data;
  }

  private sealed record StreamWriteResponse { }

  private StreamRequestHandler<StreamWriteRequest, StreamWriteResponse> StreamWrite =>
    async (request, stream, cancellationToken) =>
    {
      await stream.WriteAsync(request.Data, cancellationToken);

      return new() { };
    };
}
