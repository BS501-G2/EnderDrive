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
public enum UserRole
{
    None = 0,
    Admin = 1 << 0,
    NewsEditor = 1 << 1,
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
    public required UserRole Role;

    [JsonIgnore]
    public required byte[] RsaPublicKey;

    [JsonIgnore]
    public required ObjectId? RootFileId;

    [JsonIgnore]
    public required ObjectId? TrashFileId;

    [JsonIgnore]
    public required ObjectId? ProfilePictureId;

    [JsonIgnore]
    public required bool PrivacyPolicyAgreement;
}

[Flags]
public enum UsernameValidation
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

    public UsernameValidation ValidateUsername(string username)
    {
        UsernameValidation validation = default;

        if (username.Length < USERNAME_MIN_LENGTH)
        {
            validation |= UsernameValidation.TooShort;
        }

        if (username.Length > USERNAME_MAX_LENGTH)
        {
            validation |= UsernameValidation.TooLong;
        }

        if (!USERNAME_REGEX.IsMatch(username))
        {
            validation |= UsernameValidation.InvalidChars;
        }

        return validation;
    }

    public async Task<(
        User User,
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

        UserRole role = await Query<User>(
                transaction,
                (query) => query.Where((user) => user.Role >= UserRole.Admin)
            )
            .ToAsyncEnumerable()
            .AnyAsync(transaction.CancellationToken)
            ? UserRole.None
            : UserRole.Admin;

        User user =
            new()
            {
                Id = ObjectId.GenerateNewId(),
                Username = username,
                FirstName = firstName,
                MiddleName = middleName,
                LastName = lastName,
                DisplayName = displayName,

                Role = role,

                RsaPublicKey = rsaPublicKey,

                TrashFileId = null,
                RootFileId = null,
                ProfilePictureId = null,

                PrivacyPolicyAgreement = false
            };

        await Insert(transaction, [user]);

        return (
            user,
            await AddInitialUserAuthentication(
                transaction,
                user,
                UserAuthenticationType.Password,
                Encoding.UTF8.GetBytes(password),
                rsaPublicKey,
                rsaPrivateKey
            )
        );
    }

    public async Task SetUserProfilePictureId(
        ResourceTransaction transaction,
        User user,
        UnlockedFile? file
    )
    {
        await Update(
            transaction,
            user,
            Builders<User>.Update.Set((item) => item.ProfilePictureId, file?.Id)
        );

        user.ProfilePictureId = file?.Id;
    }

    public async Task DeleteUser(
        ResourceTransaction transaction,
        User user,
        UnlockedUserAuthentication userAuthentication
    )
    {
        await foreach (
            UserAuthentication toDelete in Query<UserAuthentication>(
                    transaction,
                    (query) =>
                        query.Where((userAuthentication) => userAuthentication.UserId == user.Id)
                )
                .ToAsyncEnumerable()
        )
        {
            await RemoveUserAuthentication(
                transaction,
                userAuthentication.Id != toDelete.Id
                    ? toDelete.Unlock(userAuthentication)
                    : userAuthentication,
                true
            );
        }

        await foreach (
            File file in Query<File>(
                    transaction,
                    (query) => query.Where((file) => file.OwnerUserId == user.Id)
                )
                .ToAsyncEnumerable()
        )
        {
            await DeleteFile(transaction, file.Unlock(userAuthentication));
        }

        await Delete(transaction, user);
    }

    public ValueTask SetUserRole(ResourceTransaction transaction, User user, UserRole role) =>
        Update(transaction, user, Builders<User>.Update.Set((item) => item.Role, role));

    public IQueryable<User> GetUsers(
        ResourceTransaction transaction,
        string? searchString = null,
        UserRole? minRole = null,
        UserRole? maxRole = null,
        string? username = null,
        ObjectId? id = null
    )
    {
        return Query<User>(
            transaction,
            (query) =>
                query.Where(
                    (user) =>
                        (
                            searchString == null
                            || (
                                user.FirstName.Contains(
                                    searchString,
                                    StringComparison.CurrentCultureIgnoreCase
                                )
                                || (
                                    user.MiddleName != null
                                    && user.MiddleName.Contains(
                                        searchString,
                                        StringComparison.CurrentCultureIgnoreCase
                                    )
                                )
                                || user.LastName.Contains(
                                    searchString,
                                    StringComparison.CurrentCultureIgnoreCase
                                )
                            )
                        )
                        && (minRole == null || user.Role >= minRole)
                        && (maxRole == null || user.Role <= maxRole)
                        && (username == null || user.Username == username)
                        && (id == null || user.Id == id)
                )
        );
    }
}
