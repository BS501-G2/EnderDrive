namespace RizzziGit.EnderDrive.Server.Connections;

using System.Threading.Tasks;
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

  private void RegisterAdminHandler<S, R>(
    ConnectionContext context,
    ServerSideRequestCode code,
    AdminRequestHandler<S, R> handler,
    UserRole[]? requiredIncludeRole = null,
    UserRole[]? requiredExcludeRole = null
  ) =>
    RegisterAuthenticatedHandler<S, R>(
      context,
      code,
      async (transaction, request, userAuthentication, me, myAdminAccess) =>
      {
        if (myAdminAccess == null)
        {
          throw new ConnectionResponseException(
            ResponseCode.InsufficientRole,
            new ConnectionResponseExceptionData.RequiredRoles()
            {
              IncludeRoles = requiredIncludeRole,
              ExcludeRoles = requiredExcludeRole,
            }
          );
        }

        return await handler(transaction, request, userAuthentication, me, myAdminAccess);
      },
      requiredIncludeRole,
      requiredExcludeRole
    );
}
