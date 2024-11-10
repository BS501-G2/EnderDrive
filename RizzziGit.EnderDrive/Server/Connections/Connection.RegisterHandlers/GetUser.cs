using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

using System.Linq;
using Newtonsoft.Json.Linq;
using Resources;

public sealed partial class Connection
{
    private sealed record class GetUserRequest
    {
        [BsonElement("userId")]
        public required ObjectId UserId;
    };

    private sealed record class GetUserResponse
    {
        [BsonElement("user")]
        public required string? User;
    };

    private TransactedRequestHandler<GetUserRequest, GetUserResponse> GetUser =>
        async (transaction, request) =>
        {
            User? user = await Resources
                .GetUsers(transaction, id: request.UserId)
                .ToAsyncEnumerable()
                .FirstOrDefaultAsync(transaction.CancellationToken);

            return new() { User = user != null ? JToken.FromObject(user).ToString() : null };
        };
}
