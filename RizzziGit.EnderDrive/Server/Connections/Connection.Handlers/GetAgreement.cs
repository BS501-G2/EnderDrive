using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

public sealed partial class Connection
{
	private sealed record class GetAgreementRequest { }

	private sealed record class GetAgreementResponse
	{
		[BsonElement(
			"agreed"
		)]
		public required bool Agreed;
	}

	private AuthenticatedRequestHandler<
		GetAgreementRequest,
		GetAgreementResponse
	> GetAgreement =>
		async (
			transaction,
			request,
			userAuthentication,
			me,
			myAdminAccess
		) =>
		{
			return new()
			{
				Agreed =
					me.PrivacyPolicyAgreement,
			};
		};
}
