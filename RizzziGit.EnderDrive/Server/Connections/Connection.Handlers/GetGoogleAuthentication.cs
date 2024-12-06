using RizzziGit.EnderDrive.Server.Services;

namespace RizzziGit.EnderDrive.Server.Connections;

using System.Linq;
using Newtonsoft.Json.Linq;
using Resources;
using RizzziGit.Commons.Memory;

public sealed partial class Connection
{
  private sealed record GetGoogleAuthenticationRequest;

  private sealed record GetGoogleAuthenticationResponse
  {
    public required GoogleAccountInfo? Info;
  }

  private AuthenticatedRequestHandler<
    GetGoogleAuthenticationRequest,
    GetGoogleAuthenticationResponse
  > GetGoogleAuthentication =>
    async (transaction, request, userAuthentication, me, myAdminAccess) =>
    {
      Resource<UserAuthentication>? googleUserAuthentication = await Internal_GetFirst(
        transaction,
        Resources.Query<UserAuthentication>(
          transaction,
          (query) =>
            query.Where(
              (userAuthentication) =>
                userAuthentication.UserId == me.Id
                && userAuthentication.Type == UserAuthenticationType.Google
            )
        )
      );

      GoogleAccountInfo? googleAccountInfo =
        googleUserAuthentication != null
          ? JToken
            .Parse(CompositeBuffer.From(googleUserAuthentication.Data.Extra).ToString())
            .ToObject<GoogleAccountInfo>()
          : null;

      return new() { Info = googleAccountInfo };
    };
}
