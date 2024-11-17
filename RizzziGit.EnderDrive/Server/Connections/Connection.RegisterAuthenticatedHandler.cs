using System.Linq;
using System.Threading.Tasks;

namespace RizzziGit.EnderDrive.Server.Connections;

using Resources;

public sealed partial class Connection
{
	private delegate Task<R> AuthenticatedRequestHandler<
		S,
		R
	>(
		ResourceTransaction transaction,
		S request,
		UnlockedUserAuthentication userAuthentication,
		User me,
		UnlockedAdminAccess? myAdminAccess
	);

	private void RegisterAuthenticatedHandler<
		S,
		R
	>(
		ConnectionContext context,
		ServerSideRequestCode code,
		AuthenticatedRequestHandler<
			S,
			R
		> handler,
		UserRole[]? requiredIncludeRole =
			null,
		UserRole[]? requiredExcludeRole =
			null
	) =>
		RegisterTransactedHandler<
			S,
			R
		>(
			context,
			code,
			async (
				transaction,
				request
			) =>
			{
				UnlockedUserAuthentication userAuthentication =
					Internal_EnsureAuthentication();
				User me =
					await Internal_Me(
						transaction,
						userAuthentication
					);

				if (
					(
						requiredIncludeRole
							!= null
						|| requiredExcludeRole
							!= null
					)
					&& !await Resources
						.GetAdminAccesses(
							transaction,
							userId: me.Id
						)
						.ToAsyncEnumerable()
						.AnyAsync(
							transaction.CancellationToken
						)
				)
				{
					if (
						requiredIncludeRole
							!= null
						&& !me
							.Roles.Intersect(
								requiredIncludeRole
							)
							.Any()
					)
					{
						throw new ConnectionResponseException(
							ResponseCode.InsufficientRole,
							new ConnectionResponseExceptionData.RequiredRoles()
							{
								ExcludeRoles =
									requiredExcludeRole,
								IncludeRoles =
									requiredIncludeRole,
							}
						);
					}

					if (
						requiredExcludeRole
							!= null
						&& me.Roles.Intersect(
								requiredExcludeRole
							)
							.Any()
					)
					{
						throw new ConnectionResponseException(
							ResponseCode.InsufficientRole,
							new ConnectionResponseExceptionData.RequiredRoles()
							{
								ExcludeRoles =
									requiredExcludeRole,
								IncludeRoles =
									requiredIncludeRole,
							}
						);
					}
				}

				AdminAccess? adminAccess =
					await Resources
						.GetAdminAccesses(
							transaction,
							userId: me.Id
						)
						.ToAsyncEnumerable()
						.FirstOrDefaultAsync(
							transaction.CancellationToken
						);

				UnlockedAdminAccess? unlockedAdminAccess =
					adminAccess?.Unlock(
						userAuthentication
					);

				return await handler(
					transaction,
					request,
					userAuthentication,
					me,
					unlockedAdminAccess
				);
			}
		);
}
