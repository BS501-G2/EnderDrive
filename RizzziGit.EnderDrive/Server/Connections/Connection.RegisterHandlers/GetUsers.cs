using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;

namespace RizzziGit.EnderDrive.Server.Connections;

using Resources;
using Utilities;

public sealed partial class Connection
{
    private sealed record class GetUsersRequest
    {
        [BsonElement("searchString")]
        public required string? SearchString;

        [BsonElement("minRole")]
        public required UserRole? MinRole;

        [BsonElement("maxRole")]
        public required UserRole? MaxRole;

        [BsonElement("username")]
        public required string? Username;

        [BsonElement("id")]
        public required ObjectId? Id;

        [BsonElement("pagination")]
        public required PaginationOptions? Pagination;
    }

    private sealed record class GetUsersResponse
    {
        [BsonElement("users")]
        public required string[] Users;
    }

    private AuthenticatedRequestHandler<GetUsersRequest, GetUsersResponse> GetUsers =>
        async (transaction, request, _, _) =>
        {
            User[] users = await Resources
                .GetUsers(
                    transaction,
                    request.SearchString,
                    request.MinRole,
                    request.MaxRole,
                    request.Username,
                    request.Id
                )
                .ApplyPagination(request.Pagination)
                .ToAsyncEnumerable()
                .ToArrayAsync(transaction.CancellationToken);

            return new()
            {
                Users = users.Select((user) => JToken.FromObject(user).ToString()).ToArray(),
            };
        };
}
