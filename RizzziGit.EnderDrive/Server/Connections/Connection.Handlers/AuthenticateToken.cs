using System;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

using Resources;

public sealed partial class Connection
{
  private sealed record class AuthenticateTokenRequest
  {
    public required ObjectId UserId;
    public required string Token;
  };

  private sealed record class AuthenticateTokenResponse
  {
    public required string? RenewedToken;
  };

  private TransactedRequestHandler<
    AuthenticateTokenRequest,
    AuthenticateTokenResponse
  > AuthenticateToken =>
    async (transaction, request) =>
    {
      if (GetContext().CurrentUser != null)
      {
        throw new InvalidOperationException("Already signed in.");
      }

      Resource<User>? user =
        await Resources
          .Query<User>(transaction, (query) => query.Where((item) => item.Id == request.UserId))
          .FirstOrDefaultAsync(transaction.CancellationToken)
        ?? throw new InvalidOperationException("Invalid user id.");

      UnlockedUserAuthentication? unlockedUserAuthentication = null;
      await foreach (
        Resource<UserAuthentication> userAuthentication in Resources
          .Query<UserAuthentication>(
            transaction,
            (query) => query.Where((item) => item.UserId == user.Id)
          )
          .Where((item) => !item.Data.IsExpired)
      )
      {
        try
        {
          unlockedUserAuthentication = UnlockedUserAuthentication.Unlock(
            userAuthentication,
            request.Token
          );

          break;
        }
        catch { }
      }

      if (unlockedUserAuthentication == null)
      {
        throw new InvalidOperationException("Invalid token.");
      }

      GetContext().CurrentUser = unlockedUserAuthentication;

      return new() { RenewedToken = null };
    };
}
