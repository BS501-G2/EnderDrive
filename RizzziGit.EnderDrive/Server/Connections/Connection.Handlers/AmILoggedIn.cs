using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

public sealed partial class Connection
{
  private sealed record class AmILoggedInRequest { }

  private sealed record class AmILoggedInResponse
  {
    [BsonElement(
      "isLoggedIn"
    )]
    public required bool IsLoggedIn;
  }

  private RequestHandler<
    AmILoggedInRequest,
    AmILoggedInResponse
  > AmILoggedIn =>
    async (
      request,
      cancellationToken
    ) =>
    {
      return new()
      {
        IsLoggedIn =
          GetContext().CurrentUser
          != null,
      };
    };
}
