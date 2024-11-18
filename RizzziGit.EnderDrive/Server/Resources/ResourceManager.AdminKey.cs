using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace RizzziGit.EnderDrive.Server.Resources;

using Services;

public record class AdminKey : ResourceData
{
  public required int Iterations;
  public required byte[] Salt;

  public required byte[] ChallengeBytes;
  public required byte[] EncryptedChallengeBytes;
}

public sealed partial class ResourceManager
{
  public async Task<UnlockedAdminKey> CreateAdminKey(
    ResourceTransaction transaction,
    string password
  )
  {
    ResourceManagerContext context = GetContext();

    int iterations = 10000;

    byte[] salt = new byte[16];
    context.RandomNumberGenerator.GetBytes(salt);

    byte[] iv = new byte[16];
    context.RandomNumberGenerator.GetBytes(iv);

    byte[] aesKey = HashPayload(
      salt,
      iterations,
      Encoding.UTF8.GetBytes(password)
    );

    byte[] challengeBytes = new byte[1024 * 8];
    byte[] encryptedChallengeBytes = KeyManager.Encrypt(aesKey, challengeBytes);

    Resource<AdminKey> adminKey = ToResource<AdminKey>(
      transaction,
      new()
      {
        Id = ObjectId.GenerateNewId(),
        Iterations = iterations,
        Salt = salt,

        ChallengeBytes = challengeBytes,
        EncryptedChallengeBytes = encryptedChallengeBytes,
      }
    );

    return new(adminKey) { AesKey = aesKey };
  }
}
