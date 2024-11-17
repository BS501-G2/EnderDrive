using System.Threading.Tasks;
using MongoDB.Bson;

namespace RizzziGit.EnderDrive.Server.Resources;

public record class PasswordResetRequest
	: ResourceData
{
	public required ObjectId UserId;
}

public sealed partial class ResourceManager
{
	public async Task<PasswordResetRequest> CreatePasswordResetRequest(
		ResourceTransaction transaction,
		User user
	)
	{
		PasswordResetRequest passwordResetRequest =
			new()
			{
				Id =
					ObjectId.GenerateNewId(),
				UserId =
					user.Id,
			};

		await Insert(
			transaction,
			passwordResetRequest
		);

		return passwordResetRequest;
	}

	public async Task AcceptPasswordResetRequest(
		ResourceTransaction transaction,
		string newPassword,
		PasswordResetRequest passwordResetRequest,
		UnlockedAdminAccess unlockedAdminAccess
	) { }
}
