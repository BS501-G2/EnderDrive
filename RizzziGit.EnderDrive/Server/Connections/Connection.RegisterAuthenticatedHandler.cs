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

  private void RegisterAuthenticatedRequestHandler<S, R>(
    string name,
    AuthenticatedRequestHandler<S, R> handler,
    UserRole[]? includeRole = null,
    UserRole[]? excludeRole = null
  ) =>
    RegisterTransactedRequestHandler<S, R>(
      name,
      async (transaction, request) =>
      {
        UnlockedUserAuthentication unlockedUserAuthentication =
          GetContext().CurrentUser ?? throw new ConnectionResponseException("Login required.");

        Resource<User>? me =
          await Internal_Me(transaction, unlockedUserAuthentication)
          ?? throw new ConnectionResponseException("Login required.");

        if (
          (includeRole != null || excludeRole != null)
          && !await Resources
            .Query<AdminAccess>(transaction, (query) => query.Where((item) => item.UserId == me.Id))
            .AnyAsync(transaction.CancellationToken)
        )
        {
          if (
            (includeRole != null && !me.Data.Roles.Intersect(includeRole).Any())
            || (excludeRole != null && me.Data.Roles.Intersect(excludeRole).Any())
          )
          {
            throw new InsufficientRoleException()
            {
              IncludeRoles = includeRole,
              ExcludeRoles = excludeRole
            };
          }
        }

        Resource<AdminAccess>? adminAccess = await Resources
          .Query<AdminAccess>(transaction, (query) => query.Where((item) => item.UserId == me.Id))
          .FirstOrDefaultAsync(transaction.CancellationToken);

        UnlockedAdminAccess? unlockedAdminAccess;
        try
        {
          unlockedAdminAccess =
            adminAccess != null
              ? UnlockedAdminAccess.Unlock(adminAccess, unlockedUserAuthentication)
              : null;
        }
        catch
        {
          unlockedAdminAccess = null;
        }

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
