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
using Services;

public enum UserAuthenticationType
{
    Password,
    Google,
    Token
}

public record class UserAuthentication : ResourceData
{
    public static implicit operator RSA(UserAuthentication user) =>
        KeyManager.DeserializeAsymmetricKey(user.RsaPublicKey);

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
    public required byte[] RsaPublicKey;

    [JsonIgnore]
    public required byte[] EncryptedRsaPrivateKey;

    public UnlockedUserAuthentication Unlock(UnlockedUserAuthentication userAuthentication) =>
        Unlock(userAuthentication.Payload);

    public UnlockedUserAuthentication Unlock(string payload) =>
        Unlock(Encoding.UTF8.GetBytes(payload));

    public UnlockedUserAuthentication Unlock(byte[] payload)
    {
        byte[] aesKey;
        {
            using Rfc2898DeriveBytes rfc2898DeriveBytes =
                new(payload, Salt, Iterations, HashAlgorithmName.SHA256);

            aesKey = rfc2898DeriveBytes.GetBytes(32);
        }


        byte[] rsaPrivateKey = KeyManager.Decrypt(aesKey, EncryptedRsaPrivateKey);

        return new()
        {
            Original = this,

            Id = Id,
            UserId = UserId,
            Type = Type,
            Iterations = Iterations,
            Salt = Salt,
            AesIv = AesIv,
            RsaPublicKey = RsaPublicKey,
            EncryptedRsaPrivateKey = EncryptedRsaPrivateKey,

            Payload = payload,
            AesKey = payload,
            RsaPrivateKey = rsaPrivateKey,
        };
    }
}

public record class UnlockedUserAuthentication : UserAuthentication
{
    public static implicit operator RSA(UnlockedUserAuthentication user) =>
        KeyManager.DeserializeAsymmetricKey(user.RsaPrivateKey);

    public required UserAuthentication Original;

    public required byte[] Payload;
    public required byte[] AesKey;
    public required byte[] RsaPrivateKey;
}

[Flags]
public enum PasswordVerification
{
    OK = 0,
    TooShort = 1 << 0,
    TooLong = 1 << 1,
    NoRequiredChars = 1 << 2,
    PasswordMismatch = 1 << 3
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
        ResourceTransaction transactionParams,
        User user,
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

        UserAuthentication userAuthentication =
            new()
            {
                Id = ObjectId.GenerateNewId(),
                UserId = user.Id,
                Type = type,
                Iterations = iterations,
                Salt = salt,
                AesIv = iv,
                RsaPublicKey = rsaPublicKey,
                EncryptedRsaPrivateKey = encryptedRsaPrivateKey,
            };

        await UserAuthentications.InsertOneAsync(
            userAuthentication,
            null,
            transactionParams.CancellationToken
        );

        return (
            new()
            {
                Original = userAuthentication,
                Id = userAuthentication.Id,
                UserId = user.Id,
                Type = UserAuthenticationType.Password,
                Iterations = iterations,
                Salt = salt,
                AesIv = iv,
                RsaPublicKey = rsaPublicKey,
                EncryptedRsaPrivateKey = encryptedRsaPrivateKey,
                Payload = payload,
                AesKey = aesKey,
                RsaPrivateKey = rsaPrivateKey,
            }
        );
    }

    public async Task<UnlockedUserAuthentication> AddUserAuthentication(
        ResourceTransaction transactionParams,
        User user,
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
            sourceUserAuthentication.RsaPrivateKey
        );

        UserAuthentication userAuthentication =
            new()
            {
                Id = ObjectId.GenerateNewId(),
                UserId = user.Id,
                Type = type,
                Iterations = iterations,
                Salt = salt,
                AesIv = iv,
                RsaPublicKey = sourceUserAuthentication.RsaPublicKey,
                EncryptedRsaPrivateKey = encryptedRsaPrivateKey,
            };

        await UserAuthentications.InsertOneAsync(
            userAuthentication,
            null,
            transactionParams.CancellationToken
        );

        return new()
        {
            Original = userAuthentication,
            Id = userAuthentication.Id,
            UserId = user.Id,
            Type = type,
            Iterations = iterations,
            Salt = salt,
            AesIv = iv,
            RsaPublicKey = sourceUserAuthentication.RsaPublicKey,
            EncryptedRsaPrivateKey = encryptedRsaPrivateKey,
            AesKey = sourceUserAuthentication.AesKey,
            RsaPrivateKey = sourceUserAuthentication.RsaPrivateKey,
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
                        userAuthentication.UserId == unlockedUserAuthentication.UserId
                )
                .ToAsyncEnumerable()
                .CountAsync(transactionParams.CancellationToken) <= 1
            && !forceDelete
        )
        {
            throw new InvalidOperationException(
                "Removing the last user authentication is not allowed"
            );
        }

        await UserAuthentications.DeleteManyAsync(
            (userAuthentication) => userAuthentication.Id == unlockedUserAuthentication.Id,
            null,
            transactionParams.CancellationToken
        );
    }

    public Task RemoveUserAuthentication(
        ResourceTransaction transactionParams,
        UnlockedUserAuthentication unlockedUserAuthentication
    ) => RemoveUserAuthentication(transactionParams, unlockedUserAuthentication, false);

    public async Task TruncateLatestToken(ResourceTransaction transaction, User user, int count)
    {
        await foreach (
            UserAuthentication userAuthentication in GetUserAuthentications(
                    transaction,
                    user,
                    UserAuthenticationType.Token
                )
                .ToAsyncEnumerable()
                .Reverse()
                .Skip(count)
        )
        {
            await Delete(transaction, userAuthentication);
        }
    }

    public IQueryable<UserAuthentication> GetUserAuthentications(
        ResourceTransaction transaction,
        User? user = null,
        UserAuthenticationType? type = null
    ) =>
        Query<UserAuthentication>(
            transaction,
            (query) =>
                query.Where(
                    (item) =>
                        (user == null || user.Id == item.UserId)
                        && (type == null || type == item.Type)
                )
        );
}
