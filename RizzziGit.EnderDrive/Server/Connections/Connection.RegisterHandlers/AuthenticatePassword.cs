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
        [BsonElement("userId")]
        public required ObjectId UserId;

        [BsonElement("password")]
        public required string Password;
    }

    private sealed record class AuthenticatePasswordResponse
    {
        [BsonElement("userId")]
        public required ObjectId UserId;

        [BsonElement("token")]
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

            User user =
                await Resources
                    .GetUsers(transaction, id: request.UserId)
                    .ToAsyncEnumerable()
                    .FirstOrDefaultAsync(transaction.CancellationToken)
                ?? throw new InvalidOperationException("Invalid username or password.");

            UserAuthentication userAuthentication =
                await Resources
                    .GetUserAuthentications(transaction, user, UserAuthenticationType.Password)
                    .ToAsyncEnumerable()
                    .FirstOrDefaultAsync(transaction.CancellationToken)
                ?? throw new InvalidOperationException("Password is unavailable for this account.");

            UnlockedUserAuthentication unlockedUserAuthentication;
            try
            {
                unlockedUserAuthentication = userAuthentication.Unlock(request.Password);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException("Invalid username or password.", exception);
            }

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

            return new() { UserId = user.Id, Token = tokenPayload.ToString() };
        };
}
