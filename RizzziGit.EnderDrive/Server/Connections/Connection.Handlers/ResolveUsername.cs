using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using RizzziGit.EnderDrive.Server.Resources;

namespace RizzziGit.EnderDrive.Server.Connections;

public sealed partial class Connection
{
  private sealed record class ResolveUsernameRequest
  {
    [BsonElement("username")]
    public required string Username;
  }

  private sealed record class ResolveUsernameResponse
  {
    [BsonElement("userId")]
    public required ObjectId? UserId;
  }

  private TransactedRequestHandler<
    ResolveUsernameRequest,
    ResolveUsernameResponse
  > ResolveUsername =>
    async (transaction, request) =>
      new()
      {
        UserId = (
          await Resources
            .Query<User>(
              transaction,
              (query) =>
                query.Where(
                  (item) =>
                    item.Username.Equals(
                      request.Username,
                      System.StringComparison.OrdinalIgnoreCase
                    )
                )
            )
            .FirstOrDefaultAsync(transaction.CancellationToken)
        )?.Id,
      };
}
