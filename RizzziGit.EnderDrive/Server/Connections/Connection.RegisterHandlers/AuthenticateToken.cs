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
        [BsonElement("userId")]
        public required ObjectId UserId;

        [BsonElement("token")]
        public required string Token;
    };

    private sealed record class AuthenticateTokenResponse
    {
        [BsonElement("renewedToken")]
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

            User? user =
                await Resources
                    .GetUsers(transaction, id: request.UserId)
                    .ToAsyncEnumerable()
                    .FirstOrDefaultAsync(transaction.CancellationToken)
                ?? throw new InvalidOperationException("Invalid user id.");

            UnlockedUserAuthentication? unlockedUserAuthentication = null;
            await foreach (
                UserAuthentication userAuthentication in Resources
                    .GetUserAuthentications(
                        transaction,
                        user: user,
                        type: UserAuthenticationType.Token
                    )
                    .ToAsyncEnumerable()
            )
            {
                try
                {
                    unlockedUserAuthentication = userAuthentication.Unlock(request.Token);
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
