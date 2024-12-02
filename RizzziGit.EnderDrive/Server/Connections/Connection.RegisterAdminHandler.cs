using System.Threading.Tasks;

namespace RizzziGit.EnderDrive.Server.Connections;

using Resources;

public sealed partial class Connection
{
  private delegate Task<R> AdminRequestHandler<S, R>(
    ResourceTransaction transaction,
    S request,
    UnlockedUserAuthentication userAuthentication,
    Resource<User> me,
    UnlockedAdminAccess myAdminAccess
  );

  private void RegisterAdminRequestHandler<S, R>(
    string name,
    AdminRequestHandler<S, R> handler,
    UserRole[]? requiredIncludeRole = null,
    UserRole[]? requiredExcludeRole = null
  ) =>
    RegisterAuthenticatedRequestHandler<S, R>(
      name,
      async (transaction, request, userAuthentication, me, myAdminAccess) =>
      {
        if (myAdminAccess == null)
        {
          throw new AdminRequired();
        }

        return await handler(transaction, request, userAuthentication, me, myAdminAccess);
      },
      requiredIncludeRole,
      requiredExcludeRole
    );
}
