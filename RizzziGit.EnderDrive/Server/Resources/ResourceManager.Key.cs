using System.Security.Cryptography;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;

namespace RizzziGit.EnderDrive.Server.Resources;

using Services;

public record class Key
  : ResourceData;

public record class KeyAccess
  : ResourceData
{
  [JsonProperty(
    "keyId"
  )]
  public required ObjectId KeyId;

  [JsonProperty(
    "userIds"
  )]
  public required ObjectId UserId;

  [JsonIgnore]
  public required byte[] EncryptedAesKey;

  public UnlockedKeyAccess Unlock(
    UnlockedUserAuthentication userAuthentication
  )
  {
    byte[] aesKey =
      KeyManager.Decrypt(
        userAuthentication,
        EncryptedAesKey
      );

    return new()
    {
      Id =
        ObjectId.GenerateNewId(),
      KeyId =
        KeyId,
      UserId =
        UserId,
      EncryptedAesKey =
        EncryptedAesKey,

      Original =
        this,

      AesKey =
        aesKey,
    };
  }
}

public record class UnlockedKeyAccess
  : KeyAccess
{
  public required KeyAccess Original;

  public required byte[] AesKey;
}

public sealed partial class ResourceManager
{
  private IMongoCollection<Key> Keys =>
    GetCollection<Key>();
  private IMongoCollection<KeyAccess> KeyAccesses =>
    GetCollection<KeyAccess>();

  public async Task<(
    Key Key,
    KeyAccess KeyAccess
  )> CreateKey(
    ResourceTransaction transactionParams,
    User user
  )
  {
    Key key =
      new()
      {
        Id =
          ObjectId.GenerateNewId(),
      };

    await Keys.InsertOneAsync(
      key,
      null,
      transactionParams.CancellationToken
    );

    return (
      key,
      await AddInitialKeyAccess(
        transactionParams,
        key,
        user
      )
    );
  }

  public async Task<UnlockedKeyAccess> AddInitialKeyAccess(
    ResourceTransaction transactionParams,
    Key key,
    User user
  )
  {
    byte[] aesKey =
      RandomNumberGenerator.GetBytes(
        32
      );
    byte[] encryptedAesKey =
      KeyManager.Encrypt(
        user,
        aesKey
      );

    KeyAccess keyAccess =
      new()
      {
        Id =
          ObjectId.GenerateNewId(),
        KeyId =
          key.Id,
        UserId =
          user.Id,
        EncryptedAesKey =
          encryptedAesKey,
      };

    await KeyAccesses.InsertOneAsync(
      keyAccess,
      null,
      transactionParams.CancellationToken
    );

    return new()
    {
      Original =
        keyAccess,

      Id =
        keyAccess.Id,
      KeyId =
        key.Id,
      UserId =
        user.Id,
      EncryptedAesKey =
        encryptedAesKey,

      AesKey =
        aesKey,
    };
  }

  public async Task<UnlockedKeyAccess> AddKeyAccess(
    ResourceTransaction transactionParams,
    Key key,
    UnlockedKeyAccess sourceKeyAccess,
    User user
  )
  {
    byte[] aesKey =
      sourceKeyAccess.AesKey;
    byte[] encryptedAesKey =
      KeyManager.Encrypt(
        user,
        aesKey
      );

    KeyAccess keyAccess =
      new()
      {
        Id =
          ObjectId.GenerateNewId(),
        KeyId =
          key.Id,
        UserId =
          user.Id,
        EncryptedAesKey =
          encryptedAesKey,
      };

    await KeyAccesses.InsertOneAsync(
      keyAccess,
      null,
      transactionParams.CancellationToken
    );

    return new()
    {
      Original =
        keyAccess,

      Id =
        keyAccess.Id,
      KeyId =
        key.Id,
      UserId =
        user.Id,
      EncryptedAesKey =
        encryptedAesKey,
      AesKey =
        aesKey,
    };
  }

  public Task RemoveKeyAccess(
    ResourceTransaction transaction,
    UnlockedKeyAccess keyAccess
  ) =>
    Delete(
      transaction,
      keyAccess
    );
}
