using Newtonsoft.Json.Linq;

namespace RizzziGit.EnderDrive.Server.Connections;

using System;
using System.Linq;
using Commons.Memory;
using Resources;

public sealed partial class Connection
{
  private sealed record SetGoogleAuthenticationRequest
  {
    public required string? Token;
  }

  private sealed record SetGoogleAuthenticationResponse { }

  private AuthenticatedRequestHandler<
    SetGoogleAuthenticationRequest,
    SetGoogleAuthenticationResponse
  > SetGoogleAuthentication =>
    async (transaction, request, userAuthentication, me, myAdminAccess) =>
    {
      byte[]? payload =
        request.Token != null
          ? await Server.GoogleService.GetPayload(request.Token, transaction)
          : null;

      if (payload != null)
      {
        UnlockedUserAuthentication? unlockedUserAuthentication = null;

        await foreach (
          Resource<UserAuthentication> a in Resources.Query<UserAuthentication>(
            transaction,
            (query) => query.Where((item) => item.Type == UserAuthenticationType.Google)
          )
        )
        {
          try
          {
            unlockedUserAuthentication = UnlockedUserAuthentication.Unlock(a, payload);

            break;
          }
          catch { }
        }

        if (unlockedUserAuthentication != null)
        {
          throw new InvalidOperationException(
            "This account is already bound to another EnderDrive account."
          );
        }
      }

      await foreach (
        Resource<UserAuthentication> googleAuthentication in Resources.Query<UserAuthentication>(
          transaction,
          (query) =>
            query.Where(
              (userAuthentication) =>
                userAuthentication.UserId == me.Id
                && userAuthentication.Type == UserAuthenticationType.Google
            )
        )
      )
      {
        await Resources.Delete(transaction, googleAuthentication);
      }

      if (payload != null && request.Token != null)
      {
        await Resources.AddUserAuthentication(
          transaction,
          me,
          userAuthentication,
          UserAuthenticationType.Google,
          payload,
          CompositeBuffer
            .From(
              JToken
                .FromObject(await Server.GoogleService.GetAccountInfo(request.Token, transaction))
                .ToString()
            )
            .ToByteArray()
        );
      }

      return new() { };
    };
}
