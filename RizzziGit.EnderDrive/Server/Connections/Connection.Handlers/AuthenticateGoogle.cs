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
    [BsonElement("token")]
    public required string Token;
  }

  private sealed record class AuthenticateGoogleResponse
  {
    [BsonElement("userId")]
    public required string UserId;

    [BsonElement("token")]
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
        UserAuthentication userAuthentication in Resources
          .GetUserAuthentications(
            transaction,
            type: UserAuthenticationType.Google
          )
          .ToAsyncEnumerable()
      )
      {
        try
        {
          unlockedUserAuthentication = userAuthentication.Unlock(payload);
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

      User user = await Resources
        .GetUsers(transaction, id: unlockedUserAuthentication.UserId)
        .ToAsyncEnumerable()
        .FirstAsync(transaction.CancellationToken);

      await Resources.TruncateLatestToken(transaction, user, 10);
      CompositeBuffer tokenPayload = CompositeBuffer.From(
        CompositeBuffer.Random(16).ToHexString()
      );

      GetContext().CurrentUser = await Resources.AddUserAuthentication(
        transaction,
        user,
        unlockedUserAuthentication,
        UserAuthenticationType.Token,
        tokenPayload.ToByteArray()
      );

      return new()
      {
        UserId = user.Id.ToString(),
        Token = tokenPayload.ToString(),
      };
    };
}
