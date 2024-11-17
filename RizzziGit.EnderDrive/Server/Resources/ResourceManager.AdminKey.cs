using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace RizzziGit.EnderDrive.Server.Resources;

using Services;

public record class AdminKey
	: ResourceData
{
	public required int Iterations;
	public required byte[] Salt;

	public required byte[] ChallengeBytes;
	public required byte[] EncryptedChallengeBytes;

	public UnlockedAdminKey Unlock(
		string password
	)
	{
		using Rfc2898DeriveBytes rfc2898DeriveBytes =
			new(
				Encoding.UTF8.GetBytes(
					password
				),
				Salt,
				Iterations,
				HashAlgorithmName.SHA256
			);
		byte[] aesKey =
			rfc2898DeriveBytes.GetBytes(
				32
			);

		byte[] decryptedChallengeBytes =
			KeyManager.Decrypt(
				aesKey,
				EncryptedChallengeBytes
			);

		return new UnlockedAdminKey()
		{
			Id =
				Id,
			Iterations =
				Iterations,
			Salt =
				Salt,

			ChallengeBytes =
				ChallengeBytes,
			EncryptedChallengeBytes =
				EncryptedChallengeBytes,

			Original =
				this,
			AesKey =
				aesKey,
		};
	}
}

public sealed record class UnlockedAdminKey
	: AdminKey
{
	public static implicit operator byte[](
		UnlockedAdminKey adminKey
	) =>
		adminKey.AesKey;

	public required AdminKey Original;

	public required byte[] AesKey;
}

public sealed partial class ResourceManager
{
	public async Task<UnlockedAdminKey> CreateAdminKey(
		ResourceTransaction transaction,
		string password
	)
	{
		ResourceManagerContext context =
			GetContext();

		int iterations =
			10000;

		byte[] salt =
			new byte[
				16
			];
		context.RandomNumberGenerator.GetBytes(
			salt
		);

		byte[] iv =
			new byte[
				16
			];
		context.RandomNumberGenerator.GetBytes(
			iv
		);

		byte[] aesKey =
			HashPayload(
				salt,
				iterations,
				Encoding.UTF8.GetBytes(
					password
				)
			);

		byte[] challengeBytes =
			new byte[
				1024
					* 8
			];
		byte[] encryptedChallengeBytes =
			KeyManager.Encrypt(
				aesKey,
				challengeBytes
			);

		AdminKey adminKey =
			new()
			{
				Id =
					ObjectId.GenerateNewId(),
				Iterations =
					iterations,
				Salt =
					salt,

				ChallengeBytes =
					challengeBytes,
				EncryptedChallengeBytes =
					encryptedChallengeBytes,
			};

		await Insert(
			transaction,
			[
				adminKey,
			]
		);

		return new()
		{
			Id =
				adminKey.Id,
			Iterations =
				adminKey.Iterations,
			Salt =
				adminKey.Salt,

			ChallengeBytes =
				adminKey.ChallengeBytes,
			EncryptedChallengeBytes =
				adminKey.EncryptedChallengeBytes,

			Original =
				adminKey,

			AesKey =
				aesKey,
		};
	}

	public async Task<AdminKey?> GetExistingAdminKey(
		ResourceTransaction transaction
	)
	{
		return await Query<AdminKey>(
				transaction,
				(
					query
				) =>
					query
			)
			.ToAsyncEnumerable()
			.FirstOrDefaultAsync(
				transaction.CancellationToken
			);
	}
}
