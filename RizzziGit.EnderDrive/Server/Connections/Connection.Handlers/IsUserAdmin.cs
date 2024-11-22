using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using RizzziGit.EnderDrive.Server.Resources;

namespace RizzziGit.EnderDrive.Server.Connections;

public sealed partial class Connection
{
  private sealed record class IsUserAdminRequest
  {
    [BsonElement("userId")]
    public required ObjectId? UserId;
  }

  private sealed record class IsUserAdminResponse
  {
    [BsonElement("isAdmin")]
    public required bool IsAdmin;
  }

  private AdminRequestHandler<IsUserAdminRequest, IsUserAdminResponse> IsUserAdmin =>
    async (transaction, request, userAuthentication, me, myAdminAccess) =>
    {
      return new()
      {
        IsAdmin = await Resources
          .Query<AdminAccess>(
            transaction,
            (query) => query.Where((item) => item.UserId == request.UserId)
          )
          .AnyAsync(transaction.CancellationToken),
      };
    };
}
