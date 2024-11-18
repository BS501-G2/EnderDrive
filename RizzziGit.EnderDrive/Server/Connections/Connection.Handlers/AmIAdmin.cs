using System.Linq;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

using Resources;

public sealed partial class Connection
{
  private sealed record class AmIAdminRequest { }

  private sealed record class AmIAdminResponse
  {
    [BsonElement("isAdmin")]
    public required bool isAdmin;
  }

  private AuthenticatedRequestHandler<AmIAdminRequest, AmIAdminResponse> AmIAdmin =>
    async (transaction, request, userAuthentication, me, myAdminAccess) =>
    {
      ConnectionContext context = GetContext();

      return new() { isAdmin = myAdminAccess != null };
    };
}
