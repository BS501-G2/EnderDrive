using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using MongoDB.Bson;
using Newtonsoft.Json;

namespace RizzziGit.EnderDrive.Server.Resources;

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
    [JsonProperty("enityType")]
    public required FileAccessTargetEntityType EntityType;

    [JsonProperty("entityId")]
    public required ObjectId EntityId;
}

public record class FileAccess : ResourceData
{
    [JsonProperty("fileId")]
    public required ObjectId FileId;

    [JsonProperty("authorUserId")]
    public required ObjectId AuthorUserId;

    [JsonProperty("targetEntity")]
    public required FileTargetEntity? TargetEntity;

    [JsonIgnore]
    public required byte[] EncryptedAesKey;

    [JsonIgnore]
    public required byte[] AesIv;

    [JsonProperty("level")]
    public required FileAccessLevel Level;

    public UnlockedFileAccess Unlock(UnlockedUserAuthentication userAuthentication)
    {
        return new()
        {
            Original = this,

            Id = Id,

            FileId = FileId,
            AuthorUserId = AuthorUserId,
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
        FileAccessLevel level,
        User authorUser
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
                AuthorUserId = authorUser.Id,
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

            AuthorUserId = access.AuthorUserId,
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
        User targetUser,
        User authorUser,
        FileAccessLevel level
    )
    {
        byte[] encryptedAesKey = KeyManager.Encrypt(targetUser, file.AesKey);
        FileAccess access =
            new()
            {
                Id = ObjectId.GenerateNewId(),

                FileId = file.Id,

                AuthorUserId = authorUser.Id,

                TargetEntity = new()
                {
                    EntityType = FileAccessTargetEntityType.User,
                    EntityId = targetUser.Id
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
            AuthorUserId = access.AuthorUserId,
            TargetEntity = access.TargetEntity,
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
        User authorUser,
        FileAccessLevel level
    )
    {
        byte[] encryptedAesKey = KeyManager.Encrypt(group, file.AesKey);
        FileAccess access =
            new()
            {
                Id = ObjectId.GenerateNewId(),

                FileId = file.Id,
                AuthorUserId = authorUser.Id,
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
            AuthorUserId = access.AuthorUserId,
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

    public IQueryable<FileAccess> GetFileAccesses(
        ResourceTransaction transaction,
        User? targetUser = null,
        Group? targetGroup = null,
        File? targetFile = null,
        User? authorUser = null,
        FileAccessLevel? level = null
    ) =>
        Query<FileAccess>(
            transaction,
            (query) =>
                query.Where(
                    (item) =>
                        (
                            targetUser == null
                            || (
                                item.TargetEntity != null
                                && item.TargetEntity.EntityType == FileAccessTargetEntityType.User
                                && targetUser.Id == item.TargetEntity.EntityId
                            )
                        )
                        && (
                            targetGroup == null
                            || (
                                item.TargetEntity != null
                                && item.TargetEntity.EntityType == FileAccessTargetEntityType.Group
                                && targetGroup.Id == item.TargetEntity.EntityId
                            )
                        )
                        && (authorUser == null || (item.AuthorUserId == authorUser.Id))
                        && (targetFile == null || targetFile.Id == item.FileId)
                        && (level == null || level <= item.Level)
                )
        );

    public async Task<FileAccessResult?> FindFileAccess(
        ResourceTransaction transaction,
        File file,
        User user,
        FileAccessLevel? minLevel = null
    )
    {
        if (file.OwnerUserId == user.Id)
        {
            return new(user, file, null);
        }

        FileAccess? access = await GetFileAccesses(transaction, targetUser: user, targetFile: file)
            .OrderByDescending(x => x.Level)
            .ToAsyncEnumerable()
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
                .ToAsyncEnumerable()
                .FirstOrDefaultAsync(transaction.CancellationToken);

            if (parentFile == null)
            {
                return null;
            }

            return await FindFileAccess(transaction, parentFile, user);
        }

        if (minLevel != null && access.Level < minLevel)
        {
            return null;
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
