using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

using Resources;

public sealed partial class Connection
{
  private sealed record class SetPositionRequest
  {
    public required ObjectId StreamId;
    public required long NewPosition;
  }

  private sealed record class SetPositionResponse { }

  private RequestHandler<SetPositionRequest, SetPositionResponse> SetPosition =>
    async (request, cancellationToken) =>
    {
      ConnectionContext context = GetContext();

      if (
        !Resources.TryGetActiveFileStream(
          request.StreamId,
          out ResourceManager.FileResourceStream? stream
        )
      )
      {
        throw new InvalidOperationException("File stream not found.");
      }

      await stream.SeekAsync(request.NewPosition, System.IO.SeekOrigin.Begin, cancellationToken);

      return new();
    };
}
