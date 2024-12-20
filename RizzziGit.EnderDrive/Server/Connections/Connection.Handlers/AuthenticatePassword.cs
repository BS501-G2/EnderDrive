using System;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

using Commons.Memory;
using Resources;

public sealed partial class Connection
{
  private sealed record class AuthenticatePasswordRequest
  {
    public required ObjectId UserId;
    public required string Password;
  }

  private sealed record class AuthenticatePasswordResponse
  {
    public required ObjectId UserId;
    public required string Token;
  }

  private TransactedRequestHandler<
    AuthenticatePasswordRequest,
    AuthenticatePasswordResponse
  > AuthenticatePassword =>
    async (transaction, request) =>
    {
      if (GetContext().CurrentUser != null)
      {
        throw new InvalidOperationException("Already signed in.");
      }

      Resource<User> user =
        await Resources
          .Query<User>(transaction, (query) => query.Where((item) => item.Id == request.UserId))
          .FirstOrDefaultAsync(transaction.CancellationToken)
        ?? throw new InvalidOperationException("Invalid username or password.");

      Resource<UserAuthentication> userAuthentication =
        await Resources
          .Query<UserAuthentication>(
            transaction,
            (query) =>
              query.Where(
                (item) =>
                  item.UserId == request.UserId && item.Type == UserAuthenticationType.Password
              )
          )
          .FirstOrDefaultAsync(transaction.CancellationToken)
        ?? throw new InvalidOperationException("Password is unavailable for this account.");

      UnlockedUserAuthentication unlockedUserAuthentication;
      try
      {
        unlockedUserAuthentication = UnlockedUserAuthentication.Unlock(
          userAuthentication,
          request.Password
        );
      }
      catch (Exception exception)
      {
        throw new InvalidOperationException("Invalid username or password.", exception);
      }

      await Resources.TruncateLatestToken(transaction, user, 10);
      CompositeBuffer tokenPayload = CompositeBuffer.From(CompositeBuffer.Random(16).ToHexString());

      GetContext().CurrentUser = await Resources.AddUserAuthentication(
        transaction,
        user,
        unlockedUserAuthentication,
        UserAuthenticationType.Token,
        tokenPayload.ToByteArray()
      );

      return new() { UserId = user.Id, Token = tokenPayload.ToString() };
    };
}
