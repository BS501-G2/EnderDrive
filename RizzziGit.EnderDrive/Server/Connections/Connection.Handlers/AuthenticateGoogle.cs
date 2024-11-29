using System;
using System.Linq;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

using Commons.Memory;
using Resources;

public sealed partial class Connection
{
  private sealed record class AuthenticateGoogleRequest
  {
    public required string Token;
  }

  private sealed record class AuthenticateGoogleResponse
  {
    public required string UserId;
    public required string Token;
  }

  private TransactedRequestHandler<
    AuthenticateGoogleRequest,
    AuthenticateGoogleResponse
  > AuthenticateGoogle =>
    async (transaction, request) =>
    {
      byte[] payload = await Server.GoogleService.GetPayload(
        request.Token,
        transaction.CancellationToken
      );

      UnlockedUserAuthentication? unlockedUserAuthentication = null;
      await foreach (
        Resource<UserAuthentication> userAuthentication in Resources.Query<UserAuthentication>(
          transaction,
          (query) => query.Where((item) => item.Type == UserAuthenticationType.Google)
        )
      )
      {
        try
        {
          unlockedUserAuthentication = UnlockedUserAuthentication.Unlock(
            userAuthentication,
            payload
          );

          break;
        }
        catch { }
      }

      if (unlockedUserAuthentication == null)
      {
        throw new InvalidOperationException(
          "No EnderDrive account associated with this Google account."
        );
      }

      Resource<User> user = await Internal_EnsureFirst(
        transaction,
        Resources.Query<User>(
          transaction,
          (query) =>
            query.Where(
              (item) => item.Id == unlockedUserAuthentication.UserAuthentication.Data.UserId
            )
        )
      );

      await Resources.TruncateLatestToken(transaction, user, 10);
      CompositeBuffer tokenPayload = CompositeBuffer.From(CompositeBuffer.Random(16).ToHexString());

      GetContext().CurrentUser = await Resources.AddUserAuthentication(
        transaction,
        user,
        unlockedUserAuthentication,
        UserAuthenticationType.Token,
        tokenPayload.ToByteArray()
      );

      return new() { UserId = user.Id.ToString(), Token = tokenPayload.ToString() };
    };
}
