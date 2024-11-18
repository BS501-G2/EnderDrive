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

  private AuthenticatedRequestHandler<GetUserRequest, GetUserResponse> GetUser =>
    async (transaction, request, _, _, _) =>
    {
      Resource<User>? user = await Resources
        .Query<User>(transaction, (query) => query.Where((user) => user.Id == request.UserId))
        .FirstOrDefaultAsync(transaction.CancellationToken);

      return new() { User = user?.ToJson() };
    };
}
