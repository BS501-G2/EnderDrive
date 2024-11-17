using System.Linq;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

using Resources;

public sealed partial class Connection
{
	private sealed record class AmIAdminRequest { }

	private sealed record class AmIAdminResponse
	{
		[BsonElement(
			"isAdmin"
		)]
		public required bool isAdmin;
	}

	private TransactedRequestHandler<
		AmIAdminRequest,
		AmIAdminResponse
	> AmIAdmin =>
		async (
			transaction,
			request
		) =>
		{
			ConnectionContext context =
				GetContext();

			UnlockedUserAuthentication userAuthentication =
				Internal_EnsureAuthentication();
			User me =
				await Internal_Me(
					transaction,
					userAuthentication
				);

			return new()
			{
				isAdmin =
					await Resources
						.GetAdminAccesses(
							transaction,
							me.Id
						)
						.ToAsyncEnumerable()
						.AnyAsync(
							transaction.CancellationToken
						),
			};
		};
}
