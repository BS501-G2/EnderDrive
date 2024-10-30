using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using MongoDB.Bson;
using Newtonsoft.Json;

namespace RizzziGit.EnderDrive.Server.Resources;

using System;
using MongoDB.Bson.Serialization.Attributes;
using Services;

public enum FileAccessLevel
{
    None,
    Read,
    ReadWrite,
    Manage,
    Full,
}

public enum FileAccessTargetEntityType
{
    User,
    Group,
}

public record class FileTargetEntity
{
    public required FileAccessTargetEntityType EntityType;
    public required ObjectId EntityId;
}

public record class FileAccess : ResourceData
{
    public required ObjectId FileId;
    public required FileTargetEntity? TargetEntity;

    [BsonIgnore]
    [JsonIgnore]
    public required byte[] EncryptedAesKey;

    [BsonIgnore]
    [JsonIgnore]
    public required byte[] AesIv;

    public required FileAccessLevel Level;

    public UnlockedFileAccess Unlock(UnlockedUserAuthentication userAuthentication)
    {
        return new()
        {
            Original = this,

            Id = Id,

            FileId = FileId,
            TargetEntity = TargetEntity,

            EncryptedAesKey = EncryptedAesKey,
            AesIv = AesIv,

            AesKey =
                TargetEntity != null
                    ? KeyManager.Decrypt(userAuthentication, EncryptedAesKey)
                    : EncryptedAesKey,

            Level = Level,
        };
    }
}

public record class UnlockedFileAccess : FileAccess
{
    public static implicit operator Aes(UnlockedFileAccess file) =>
        KeyManager.DeserializeSymmetricKey([.. file.AesKey, .. file.AesIv]);

    public required FileAccess Original;

    public required byte[] AesKey;

    public UnlockedFile UnlockFile(File file)
    {
        return file.WithAesKey(AesKey);
    }
}

public sealed partial class ResourceManager
{
    public async Task<UnlockedFileAccess> CreateFileAccess(
        ResourceTransaction transaction,
        UnlockedFile file,
        FileAccessLevel level
    )
    {
        if (level >= FileAccessLevel.Manage)
        {
            throw new ArgumentException(
                $"Level must be lower than {FileAccessLevel.Manage}.",
                nameof(level)
            );
        }

        FileAccess access =
            new()
            {
                Id = ObjectId.GenerateNewId(),
                FileId = file.Id,
                TargetEntity = null,
                EncryptedAesKey = file.AesKey,
                AesIv = file.AesIv,
                Level = level
            };

        await Insert(transaction, access);

        return new()
        {
            Original = access,

            Id = access.Id,
            FileId = access.FileId,

            TargetEntity = access.TargetEntity,

            EncryptedAesKey = access.EncryptedAesKey,
            AesIv = access.AesIv,

            Level = access.Level,
            AesKey = access.EncryptedAesKey
        };
    }

    public async Task<UnlockedFileAccess> CreateFileAccess(
        ResourceTransaction transaction,
        UnlockedFile file,
        UserAuthentication userAuthentication,
        FileAccessLevel level
    )
    {
        byte[] encryptedAesKey = KeyManager.Encrypt(userAuthentication, file.AesKey);
        FileAccess access =
            new()
            {
                Id = ObjectId.GenerateNewId(),

                FileId = file.Id,

                TargetEntity = new()
                {
                    EntityType = FileAccessTargetEntityType.User,
                    EntityId = userAuthentication.UserId
                },

                EncryptedAesKey = encryptedAesKey,
                AesIv = file.AesIv,

                Level = level,
            };

        await Insert(transaction, [access]);

        return new()
        {
            Original = access,

            Id = access.Id,

            FileId = access.FileId,
            TargetEntity = new()
            {
                EntityType = FileAccessTargetEntityType.User,
                EntityId = userAuthentication.UserId
            },

            EncryptedAesKey = access.EncryptedAesKey,
            AesIv = access.AesIv,

            Level = access.Level,

            AesKey = file.AesKey,
        };
    }

    public async Task<UnlockedFileAccess> CreateFileAccess(
        ResourceTransaction transaction,
        UnlockedFile file,
        Group group,
        FileAccessLevel level
    )
    {
        byte[] encryptedAesKey = KeyManager.Encrypt(group, file.AesKey);
        FileAccess access =
            new()
            {
                Id = ObjectId.GenerateNewId(),

                FileId = file.Id,
                TargetEntity = new()
                {
                    EntityType = FileAccessTargetEntityType.Group,
                    EntityId = group.Id
                },

                EncryptedAesKey = encryptedAesKey,
                AesIv = file.AesIv,

                Level = level,
            };

        await Insert(transaction, [access]);

        return new()
        {
            Original = access,

            Id = access.Id,

            FileId = access.FileId,
            TargetEntity = new()
            {
                EntityType = FileAccessTargetEntityType.Group,
                EntityId = group.Id
            },

            EncryptedAesKey = access.EncryptedAesKey,
            AesIv = access.AesIv,

            Level = access.Level,

            AesKey = file.AesKey,
        };
    }

    public IAsyncEnumerable<FileAccess> GetFileAccesses(
        ResourceTransaction transaction,
        User? user = null,
        Group? group = null,
        File? file = null
    ) =>
        Query<FileAccess>(
            transaction,
            (query) =>
                query.Where(
                    (item) =>
                        (
                            user == null
                            || (
                                item.TargetEntity != null
                                && item.TargetEntity.EntityType == FileAccessTargetEntityType.User
                                && user.Id == item.TargetEntity.EntityId
                            )
                        )
                        && (
                            group == null
                            || (
                                item.TargetEntity != null
                                && item.TargetEntity.EntityType == FileAccessTargetEntityType.Group
                                && group.Id == item.TargetEntity.EntityId
                            )
                        )
                        && (file == null || file.Id == item.FileId)
                )
        );

    public async Task<FileAccessResult?> FindFileAccess(
        ResourceTransaction transaction,
        File file,
        User user
    )
    {
        if (file.OwnerUserId == user.Id)
        {
            return new(user, file, null);
        }

        FileAccess? access = await GetFileAccesses(transaction, user: user, file: file)
            .FirstOrDefaultAsync(transaction.CancellationToken);

        if (access == null)
        {
            if (file.ParentId == null)
            {
                return null;
            }

            File? parentFile = await Query<File>(
                    transaction,
                    (query) => query.Where((item) => item.Id == file.ParentId)
                )
                .FirstOrDefaultAsync(transaction.CancellationToken);

            if (parentFile == null)
            {
                return null;
            }

            return await FindFileAccess(transaction, parentFile, user);
        }

        return new(user, file, access);
    }
}

public sealed record FileAccessResult(User User, File File, FileAccess? FileAccess)
{
    public FileAccessLevel AccessLevel =>
        File.OwnerUserId == User.Id
            ? FileAccessLevel.Full
            : FileAccess?.Level ?? FileAccessLevel.None;

    public Aes Unlock(UnlockedUserAuthentication userAuthentication)
    {
        if (File.OwnerUserId == userAuthentication.UserId)
        {
            return File.Unlock(userAuthentication);
        }
        else if (FileAccess != null)
        {
            return FileAccess.Unlock(userAuthentication);
        }

        throw new InvalidOperationException("Failed to unlock file access.");
    }
}
