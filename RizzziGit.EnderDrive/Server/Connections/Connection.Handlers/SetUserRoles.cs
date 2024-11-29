namespace RizzziGit.EnderDrive.Server.Connections;

using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Resources;

public sealed partial class Connection
{
  private sealed record class SetUserRolesRequest
  {
    public required ObjectId UserId;
    public required UserRole[] Roles;
  }

  private sealed record class SetUserRolesResponse { }

  private AdminRequestHandler<SetUserRolesRequest, SetUserRolesResponse> SetUserRoles =>
    async (transaction, request, userAuthentication, me, myAdminAccess) =>
    {
      Resource<User> user = await Internal_EnsureFirst(
        transaction,
        Resources.Query<User>(
          transaction,
          (query) => query.Where((item) => item.Id == request.UserId)
        )
      );

      user.Data.Roles = request.Roles;
      await user.Save(transaction);

      return new() { };
    };
}
