using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

using Utilities;

public sealed partial class Connection
{
  private sealed record class MeRequest() { };

  private sealed record class MeResponse
  {
    [BsonElement(
      "user"
    )]
    public required string? User;
  }

  private AuthenticatedRequestHandler<
    MeRequest,
    MeResponse
  > Me =>
    async (
      _,
      _,
      _,
      me,
      _
    ) =>
      new()
      {
        User =
          me.ToJson(),
      };
}
