using System.Text.Json.Serialization;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace RizzziGit.EnderDrive.Server.Resources;

using System.Linq;
using Services;

public record class AdminAccess
	: ResourceData
{
	[JsonIgnore]
	public required ObjectId UserId;

	[JsonIgnore]
	public required byte[] EncryptedAesKey;

	public UnlockedAdminAccess WithAesKey(
		byte[] aesKey
	)
	{
		return new()
		{
			Id =
				Id,

			UserId =
				UserId,

			EncryptedAesKey =
				EncryptedAesKey,
			AdminAesKey =
				aesKey,

			Original =
				this,
		};
	}

	public UnlockedAdminAccess Unlock(
		UnlockedUserAuthentication userAuthentication
	)
	{
		byte[] aesKey =
			KeyManager.Decrypt(
				userAuthentication,
				EncryptedAesKey
			);

		return WithAesKey(
			aesKey
		);
	}
}

public record class UnlockedAdminAccess
	: AdminAccess
{
	public static implicit operator byte[](
		UnlockedAdminAccess adminClass
	) =>
		adminClass.AdminAesKey;

	[JsonIgnore]
	public required AdminAccess Original;

	[JsonIgnore]
	public required byte[] AdminAesKey;
}

public sealed partial class ResourceManager
{
	public async Task<AdminAccess> AddAdminAccess(
		ResourceTransaction transaction,
		UnlockedAdminKey adminKey,
		User user
	)
	{
		AdminAccess adminAccess =
			new()
			{
				Id =
					ObjectId.GenerateNewId(),

				UserId =
					user.Id,

				EncryptedAesKey =

					[],
			};

		await Insert(
			transaction,
			[
				adminAccess,
			]
		);

		return adminAccess.WithAesKey(
			KeyManager.Encrypt(
				user,
				adminKey.AesKey
			)
		);
	}

	public IQueryable<AdminAccess> GetAdminAccesses(
		ResourceTransaction transaction,
		ObjectId? userId =
			null,
		ObjectId? id =
			null
	) =>
		Query<AdminAccess>(
			transaction,
			(
				query
			) =>
				query.Where(
					(
						item
					) =>
						(
							userId
								== null
							|| item.UserId
								== userId
						)
						&& (
							id
								== null
							|| item.Id
								== id
						)
				)
		);
}
