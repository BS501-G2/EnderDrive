using System.Linq;
using System.Threading.Tasks;

namespace RizzziGit.EnderDrive.Server.Connections;

using Resources;

public sealed partial class Connection
{
  private delegate Task<R> AuthenticatedRequestHandler<S, R>(
    ResourceTransaction transaction,
    S request,
    UnlockedUserAuthentication userAuthentication,
    Resource<User> me,
    UnlockedAdminAccess? myAdminAccess
  );

  private void RegisterAuthenticatedHandler<S, R>(
    ConnectionContext context,
    ServerSideRequestCode code,
    AuthenticatedRequestHandler<S, R> handler,
    UserRole[]? requiredIncludeRole = null,
    UserRole[]? requiredExcludeRole = null
  ) =>
    RegisterTransactedHandler<S, R>(
      context,
      code,
      async (transaction, request) =>
      {
        UnlockedUserAuthentication unlockedUserAuthentication =
          GetContext().CurrentUser
          ?? throw new ConnectionResponseException(
            ResponseCode.AuthenticationRequired,
            new ConnectionResponseExceptionData.AuthenticationRequired()
          );

        Resource<User>? me =
          await Internal_Me(transaction, unlockedUserAuthentication)
          ?? throw new ConnectionResponseException(
            ResponseCode.AuthenticationRequired,
            new ConnectionResponseExceptionData.AuthenticationRequired()
          );

        if (
          (requiredIncludeRole != null || requiredExcludeRole != null)
          && !await Resources
            .Query<AdminAccess>(transaction, (query) => query.Where((item) => item.UserId == me.Id))
            .AnyAsync(transaction.CancellationToken)
        )
        {
          if (requiredIncludeRole != null && !me.Data.Roles.Intersect(requiredIncludeRole).Any())
          {
            throw new ConnectionResponseException(
              ResponseCode.InsufficientRole,
              new ConnectionResponseExceptionData.RequiredRoles()
              {
                ExcludeRoles = requiredExcludeRole,
                IncludeRoles = requiredIncludeRole,
              }
            );
          }

          if (requiredExcludeRole != null && me.Data.Roles.Intersect(requiredExcludeRole).Any())
          {
            throw new ConnectionResponseException(
              ResponseCode.InsufficientRole,
              new ConnectionResponseExceptionData.RequiredRoles()
              {
                ExcludeRoles = requiredExcludeRole,
                IncludeRoles = requiredIncludeRole,
              }
            );
          }
        }

        Resource<AdminAccess>? adminAccess = await Resources
          .Query<AdminAccess>(transaction, (query) => query.Where((item) => item.UserId == me.Id))
          .FirstOrDefaultAsync(transaction.CancellationToken);

        UnlockedAdminAccess? unlockedAdminAccess =
          adminAccess != null
            ? UnlockedAdminAccess.Unlock(adminAccess, unlockedUserAuthentication)
            : null;

        return await handler(
          transaction,
          request,
          unlockedUserAuthentication,
          me,
          unlockedAdminAccess
        );
      }
    );
}
