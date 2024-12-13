using MongoDB.Bson.Serialization.Attributes;
using RizzziGit.EnderDrive.Server.Resources;

namespace RizzziGit.EnderDrive.Server.Connections;

public sealed partial class Connection
{
  private sealed record class AmILoggedInRequest { }

  private sealed record class AmILoggedInResponse
  {
    public required bool IsLoggedIn;
  }

  private RequestHandler<AmILoggedInRequest, AmILoggedInResponse> AmILoggedIn =>
    async (request, cancellationToken) =>
    {
      UnlockedUserAuthentication? unlockedUserAuthentication = GetContext().CurrentUser;

      return new()
      {
        IsLoggedIn =
          unlockedUserAuthentication != null
          && !unlockedUserAuthentication.UserAuthentication.Data.IsExpired,
      };
    };
}
