using System.Linq;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

public sealed partial class Connection
{
	private sealed record class SetupRequirementsRequest { };

	private sealed record class SetupRequirementsResponse
	{
		[BsonElement(
			"adminSetupRequired"
		)]
		public required bool AdminSetupRequired;
	};

	private TransactedRequestHandler<
		SetupRequirementsRequest,
		SetupRequirementsResponse
	> SetupRequirements =>
		async (
			transaction,
			request
		) =>
			new()
			{
				AdminSetupRequired =
					!await Resources
						.GetAdminAccesses(
							transaction
						)
						.ToAsyncEnumerable()
						.AnyAsync(
							transaction.CancellationToken
						),
			};
}
