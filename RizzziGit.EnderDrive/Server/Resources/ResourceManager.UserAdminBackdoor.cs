using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace RizzziGit.EnderDrive.Server.Resources;

using Services;

public record class UserAdminBackdoor : ResourceData
{
  public required ObjectId UserId;

  public required byte[] EncryptedUserPrivateRsaKey;
}

public sealed partial class ResourceManager
{
  public async Task<UnlockedUserAdminBackdoor> CreateUserAdminBackdoor(
    ResourceTransaction transaction,
    Resource<User> user,
    UnlockedUserAuthentication userAuthentication,
    UnlockedAdminKey adminKey
  )
  {
    byte[] userPrivateRsaKey = userAuthentication.UserRsaPrivateKey;
    Resource<UserAdminBackdoor> item = ToResource<UserAdminBackdoor>(
      transaction,
      new()
      {
        UserId = user.Id,
        EncryptedUserPrivateRsaKey = KeyManager.Encrypt(adminKey, userPrivateRsaKey),
      }
    );

    await item.Save(transaction);
    return new(item) { UserPrivateRsaKey = userPrivateRsaKey };
  }
}
