using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;

namespace RizzziGit.EnderDrive.Server.Resources;

using System.Text.RegularExpressions;
using MongoDB.Bson.Serialization.Attributes;
using Services;

public enum UserAuthenticationType
{
  Password,
  Google,
  Token,
}

public record class UserAuthentication : ResourceData
{
  public static implicit operator RSA(UserAuthentication user) =>
    KeyManager.DeserializeAsymmetricKey(user.UserPublicRsaKey);

  [JsonProperty("userId")]
  public required ObjectId UserId;

  [JsonProperty("type")]
  public required UserAuthenticationType Type;

  [JsonIgnore]
  public required int Iterations;

  [JsonIgnore]
  public required byte[] Salt;

  [JsonIgnore]
  public required byte[] AesIv;

  [JsonIgnore]
  public required byte[] UserPublicRsaKey;

  [JsonIgnore]
  public required byte[] EncryptedUserPrivateRsaKey;

  [JsonIgnore]
  [BsonRepresentation(representation: BsonType.DateTime)]
  public required DateTimeOffset CreateTime;
}

[Flags]
public enum PasswordVerification
{
  OK = 0,
  TooShort = 1 << 0,
  TooLong = 1 << 1,
  NoRequiredChars = 1 << 2,
  PasswordMismatch = 1 << 3,
}

public sealed partial class ResourceManager
{
  [GeneratedRegex(
    "^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[[$&+,:;=?@#|'<>.^*()%!\\]-])[A-Za-z\\d$&+,:;=?@#|'<>.^*()%!\\]-]*$"
  )]
  private static partial Regex GetPasswordRegex();

  public static readonly Regex PASSWORD_REGEX = GetPasswordRegex();

  private IMongoCollection<UserAuthentication> UserAuthentications =>
    GetCollection<UserAuthentication>();

  private static byte[] HashPayload(byte[] salt, int iterations, byte[] payload)
  {
    using Rfc2898DeriveBytes rfc2898DeriveBytes =
      new(payload, salt, iterations, HashAlgorithmName.SHA256);

    return rfc2898DeriveBytes.GetBytes(32);
  }

  public PasswordVerification VerifyPassword(string password, string? confirmPassword = null)
  {
    PasswordVerification verification = 0;

    if (password.Length < 6)
    {
      verification |= PasswordVerification.TooShort;
    }

    if (password.Length > 36)
    {
      verification |= PasswordVerification.TooLong;
    }

    if (!PASSWORD_REGEX.IsMatch(password))
    {
      verification |= PasswordVerification.NoRequiredChars;
    }

    if (confirmPassword != null && password != confirmPassword)
    {
      verification |= PasswordVerification.PasswordMismatch;
    }

    return verification;
  }

  private async Task<UnlockedUserAuthentication> AddInitialUserAuthentication(
    ResourceTransaction transaction,
    Resource<User> user,
    UserAuthenticationType type,
    byte[] payload,
    byte[] rsaPublicKey,
    byte[] rsaPrivateKey
  )
  {
    ResourceManagerContext context = GetContext();

    int iterations = 10000;

    byte[] salt = new byte[16];
    context.RandomNumberGenerator.GetBytes(salt);

    byte[] iv = new byte[16];
    context.RandomNumberGenerator.GetBytes(iv);

    byte[] aesKey = HashPayload(salt, iterations, payload);

    byte[] encryptedRsaPrivateKey = KeyManager.Encrypt(aesKey, rsaPrivateKey);

    Resource<UserAuthentication> userAuthentication = ToResource<UserAuthentication>(
      transaction,
      new()
      {
        Id = ObjectId.GenerateNewId(),
        UserId = user.Id,
        Type = type,
        Iterations = iterations,
        Salt = salt,
        AesIv = iv,
        UserPublicRsaKey = rsaPublicKey,
        EncryptedUserPrivateRsaKey = encryptedRsaPrivateKey,
        CreateTime = DateTimeOffset.UtcNow,
      }
    );

    await userAuthentication.Save(transaction);

    return (
      new(userAuthentication)
      {
        Payload = payload,
        AesKey = aesKey,
        UserRsaPrivateKey = rsaPrivateKey,
      }
    );
  }

  public async Task<UnlockedUserAuthentication> AddUserAuthentication(
    ResourceTransaction transaction,
    Resource<User> user,
    UnlockedUserAuthentication sourceUserAuthentication,
    UserAuthenticationType type,
    byte[] payload
  )
  {
    ResourceManagerContext context = GetContext();

    int iterations = 10000;

    byte[] salt = new byte[16];
    context.RandomNumberGenerator.GetBytes(salt);

    byte[] iv = new byte[16];
    context.RandomNumberGenerator.GetBytes(iv);

    byte[] aesKey = HashPayload(salt, iterations, payload);

    byte[] encryptedRsaPrivateKey = KeyManager.Encrypt(
      aesKey,
      sourceUserAuthentication.UserRsaPrivateKey
    );

    Resource<UserAuthentication> userAuthentication = ToResource<UserAuthentication>(
      transaction,
      new()
      {
        Id = ObjectId.GenerateNewId(),
        UserId = user.Id,
        Type = type,
        Iterations = iterations,
        Salt = salt,
        AesIv = iv,
        UserPublicRsaKey = sourceUserAuthentication.UserAuthentication.Data.UserPublicRsaKey,
        EncryptedUserPrivateRsaKey = encryptedRsaPrivateKey,
        CreateTime = DateTimeOffset.UtcNow,
      }
    );

    await userAuthentication.Save(transaction);
    return new(userAuthentication)
    {
      AesKey = sourceUserAuthentication.AesKey,
      UserRsaPrivateKey = sourceUserAuthentication.UserRsaPrivateKey,
      Payload = payload,
    };
  }

  public async Task<UnlockedUserAuthentication> AddUserAuthentication(
    ResourceTransaction transaction,
    Resource<User> user,
    UnlockedUserAdminBackdoor userBackdoor,
    UserAuthenticationType type,
    byte[] payload
  )
  {
    ResourceManagerContext context = GetContext();

    int iterations = 10000;

    byte[] salt = new byte[16];
    context.RandomNumberGenerator.GetBytes(salt);

    byte[] iv = new byte[16];
    context.RandomNumberGenerator.GetBytes(iv);

    byte[] aesKey = HashPayload(salt, iterations, payload);

    byte[] encryptedUserRsaPrivateKey = KeyManager.Encrypt(aesKey, userBackdoor.UserPrivateRsaKey);

    Resource<UserAuthentication> userAuthentication = ToResource<UserAuthentication>(
      transaction,
      new()
      {
        Id = ObjectId.GenerateNewId(),
        UserId = user.Id,
        Type = type,
        Iterations = iterations,
        Salt = salt,
        AesIv = iv,
        UserPublicRsaKey = user.Data.RsaPublicKey,
        EncryptedUserPrivateRsaKey = encryptedUserRsaPrivateKey,
        CreateTime = DateTimeOffset.UtcNow,
      }
    );

    await userAuthentication.Save(transaction);

    return new(userAuthentication)
    {
      AesKey = aesKey,
      UserRsaPrivateKey = userBackdoor.UserPrivateRsaKey,
      Payload = payload,
    };
  }

  private async Task RemoveUserAuthentication(
    ResourceTransaction transactionParams,
    UnlockedUserAuthentication unlockedUserAuthentication,
    bool forceDelete = false
  )
  {
    if (
      await UserAuthentications
        .AsQueryable()
        .Where(
          (userAuthentication) =>
            userAuthentication.UserId == unlockedUserAuthentication.UserAuthentication.Data.UserId
        )
        .ToAsyncEnumerable()
        .CountAsync(transactionParams.CancellationToken) <= 1
      && !forceDelete
    )
    {
      throw new InvalidOperationException("Removing the last user authentication is not allowed");
    }

    await UserAuthentications.DeleteManyAsync(
      (userAuthentication) =>
        userAuthentication.Id == unlockedUserAuthentication.UserAuthentication.Id,
      null,
      transactionParams.CancellationToken
    );
  }

  public Task RemoveUserAuthentication(
    ResourceTransaction transactionParams,
    UnlockedUserAuthentication unlockedUserAuthentication
  ) => RemoveUserAuthentication(transactionParams, unlockedUserAuthentication, false);

  public async Task TruncateLatestToken(
    ResourceTransaction transaction,
    Resource<User> user,
    int count
  )
  {
    await foreach (
      Resource<UserAuthentication> userAuthentication in Query<UserAuthentication>(
        transaction,
        (query) =>
          query
            .Where(
              (item) => item.UserId == user.Data.Id && item.Type == UserAuthenticationType.Token
            )
            .OrderByDescending((userAuthentication) => userAuthentication.CreateTime)
            .Skip(count)
      )
    )
    {
      await Delete(transaction, userAuthentication);
    }
  }
}
