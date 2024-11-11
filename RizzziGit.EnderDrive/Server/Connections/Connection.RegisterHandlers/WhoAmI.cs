using System.Threading.Tasks;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

public sealed partial class Connection
{
    private sealed record class WhoAmIRequest() { };

    private sealed record class WhoAmIResponse
    {
        [BsonElement("userId")]
        public required string? UserId;
    }

    private RequestHandler<WhoAmIRequest, WhoAmIResponse> WhoAmI =>
        (_, _) =>
            Task.FromResult<WhoAmIResponse>(
                new() { UserId = GetContext().CurrentUser?.UserId.ToString() }
            );
}
