using System.Linq;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

public sealed partial class Connection
{
  private sealed record class AmIAdminRequest { }

  private sealed record class AmIAdminResponse
  {
    public required bool IsAdmin;
  }

  private AuthenticatedRequestHandler<AmIAdminRequest, AmIAdminResponse> AmIAdmin =>
    async (transaction, request, userAuthentication, me, myAdminAccess) =>
    {
      ConnectionContext context = GetContext();

      return new() { IsAdmin = myAdminAccess != null };
    };
}
