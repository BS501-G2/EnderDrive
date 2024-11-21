using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;

namespace RizzziGit.EnderDrive.Server.Resources;

using Services;

[Flags]
public enum UserRole : byte
{
  None = 0b01,
  NewsEditor = 0b10,
}

public record class User : ResourceData
{
  public static implicit operator RSA(User user) =>
    KeyManager.DeserializeAsymmetricKey(user.RsaPublicKey);

  [JsonProperty("username")]
  public required string Username;

  [JsonProperty("firstName")]
  public required string FirstName;

  [JsonProperty("middleName")]
  public required string? MiddleName;

  [JsonProperty("lastName")]
  public required string LastName;

  [JsonProperty("displayName")]
  public required string? DisplayName;

  [JsonProperty("role")]
  public required UserRole[] Roles;

  [JsonIgnore]
  public required byte[] RsaPublicKey;

  [JsonIgnore]
  public required byte[] AdminEncryptedRsaPrivateKey;

  [JsonIgnore]
  public required ObjectId? RootFileId;

  [JsonIgnore]
  public required ObjectId? TrashFileId;

  [JsonIgnore]
  public required ObjectId? ProfilePictureId;

  [JsonIgnore]
  public required ObjectId? LatestNewsId;

  [JsonIgnore]
  public required bool PrivacyPolicyAgreement;
}

[Flags]
public enum UsernameValidationFlags
{
  OK = 0,
  TooShort = 1 << 0,
  TooLong = 1 << 1,
  InvalidChars = 1 << 2,
}

public sealed partial class ResourceManager
{
  [GeneratedRegex("^\\w*$")]
  private static partial Regex GetUsernameRegex();

  public static readonly Regex USERNAME_REGEX = GetUsernameRegex();

  public const int USERNAME_MIN_LENGTH = 6;
  public const int USERNAME_MAX_LENGTH = 12;

  public UsernameValidationFlags ValidateUsername(string username)
  {
    UsernameValidationFlags validation = default;

    if (username.Length < USERNAME_MIN_LENGTH)
    {
      validation |= UsernameValidationFlags.TooShort;
    }

    if (username.Length > USERNAME_MAX_LENGTH)
    {
      validation |= UsernameValidationFlags.TooLong;
    }

    if (!USERNAME_REGEX.IsMatch(username))
    {
      validation |= UsernameValidationFlags.InvalidChars;
    }

    return validation;
  }

  public async Task<(
    Resource<User> User,
    UnlockedUserAuthentication UnlockedUserAuthentication
  )> CreateUser(
    ResourceTransaction transaction,
    string username,
    string firstName,
    string? middleName,
    string lastName,
    string? displayName,
    string password
  )
  {
    transaction.CancellationToken.ThrowIfCancellationRequested();

    RSA rsaKey = await KeyManager.GenerateAsymmetricKey(transaction.CancellationToken);

    byte[] rsaPublicKey = KeyManager.SerializeAsymmetricKey(rsaKey, false);
    byte[] rsaPrivateKey = KeyManager.SerializeAsymmetricKey(rsaKey, true);

    Resource<User> user = ToResource<User>(
      transaction,
      new()
      {
        Username = username,
        FirstName = firstName,
        MiddleName = middleName,
        LastName = lastName,
        DisplayName = displayName,

        Roles = [],

        RsaPublicKey = rsaPublicKey,
        AdminEncryptedRsaPrivateKey = KeyManager.Encrypt(AdminManager.AdminKey, rsaPrivateKey),

        TrashFileId = null,
        RootFileId = null,
        ProfilePictureId = null,
        LatestNewsId = null,

        PrivacyPolicyAgreement = false,
      }
    );

    await user.Save(transaction);

    if (!await Query<AdminAccess>(transaction).AnyAsync(transaction))
    {
      await AddAdminAccess(transaction, Server.AdminManager.AdminKey, user);
    }

    UnlockedUserAuthentication userAuthentication = await AddInitialUserAuthentication(
      transaction,
      user,
      UserAuthenticationType.Password,
      Encoding.UTF8.GetBytes(password),
      rsaPublicKey,
      rsaPrivateKey
    );

    await CreateUserAdminBackdoor(
      transaction,
      user,
      userAuthentication,
      Server.AdminManager.AdminKey
    );

    return (user, userAuthentication);
  }

  public async Task Delete(ResourceTransaction transaction, Resource<User> user)
  {
    await foreach (
      Resource<UserAuthentication> userAuthentication in Query<UserAuthentication>(
        transaction,
        (query) => query.Where((userAuthentication) => userAuthentication.UserId == user.Id)
      )
    )
    {
      await Delete(transaction, userAuthentication);
    }

    await foreach (
      Resource<File> file in Query<File>(
        transaction,
        (query) => query.Where((file) => file.OwnerUserId == user.Id && file.ParentId == null)
      )
    )
    {
      await Delete(transaction, file);
    }

    await foreach (
      Resource<UserAdminBackdoor> userAdminBackdoor in Query<UserAdminBackdoor>(
        transaction,
        (query) => query.Where((item) => item.UserId == user.Id)
      )
    )
    {
      await Delete(transaction, userAdminBackdoor);
    }

    await foreach (
      Resource<FileStar> fileStar in Query<FileStar>(
        transaction,
        (query) => query.Where((item) => item.UserId == user.Id)
      )
    )
    {
      await Delete(transaction, fileStar);
    }

    await Delete<User>(transaction, user);
  }
}
