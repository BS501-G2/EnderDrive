using System.Text.Json.Serialization;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace RizzziGit.EnderDrive.Server.Resources;

using System.Linq;
using Services;

public record class AdminAccess : ResourceData
{
  [JsonIgnore]
  public required ObjectId UserId;

  [JsonIgnore]
  public required byte[] EncryptedAesKey;
}

public sealed partial class ResourceManager
{
  public async Task<UnlockedAdminAccess> AddAdminAccess(
    ResourceTransaction transaction,
    UnlockedAdminKey adminKey,
    Resource<User> user
  )
  {
    Resource<AdminAccess> adminAccess = ToResource<AdminAccess>(
      transaction,
      new()
      {
        UserId = user.Id,

        EncryptedAesKey = KeyManager.Encrypt(user.Data, adminKey.AesKey),
      }
    );

    await adminAccess.Save(transaction);
    return new(adminAccess) { AdminAesKey = adminKey.AesKey };
  }
}
