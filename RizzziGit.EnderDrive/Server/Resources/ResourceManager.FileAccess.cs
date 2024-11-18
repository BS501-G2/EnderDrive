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

  [JsonProperty("level")]
  public required FileAccessLevel Level;
}

public sealed partial class ResourceManager
{
  public async Task<UnlockedFileAccess> CreateFileAccess(
    ResourceTransaction transaction,
    UnlockedFile file,
    FileAccessLevel level,
    Resource<User> authorUser
  )
  {
    if (level >= FileAccessLevel.Manage)
    {
      throw new ArgumentException(
        $"Level must be lower than {FileAccessLevel.Manage}.",
        nameof(level)
      );
    }

    Resource<FileAccess> access = ToResource<FileAccess>(
      transaction,
      new()
      {
        Id = ObjectId.GenerateNewId(),
        FileId = file.File.Id,
        AuthorUserId = authorUser.Id,
        TargetEntity = null,
        EncryptedAesKey = file.AesKey,
        Level = level,
      }
    );

    await access.Save(transaction);

    return new(access) { AesKey = access.Data.EncryptedAesKey };
  }

  public async Task<UnlockedFileAccess> CreateFileAccess(
    ResourceTransaction transaction,
    UnlockedFile file,
    Resource<User> targetUser,
    Resource<User> authorUser,
    FileAccessLevel level
  )
  {
    byte[] encryptedAesKey = KeyManager.Encrypt(targetUser.Data, file.AesKey);

    Resource<FileAccess> access = ToResource<FileAccess>(
      transaction,
      new()
      {
        Id = ObjectId.GenerateNewId(),

        FileId = file.File.Data.Id,

        AuthorUserId = authorUser.Id,

        TargetEntity = new()
        {
          EntityType = FileAccessTargetEntityType.User,
          EntityId = targetUser.Id,
        },

        EncryptedAesKey = encryptedAesKey,

        Level = level,
      }
    );

    await access.Save(transaction);
    return new(access) { AesKey = file.AesKey };
  }

  public async Task<UnlockedFileAccess> CreateFileAccess(
    ResourceTransaction transaction,
    UnlockedFile file,
    Resource<Group> group,
    Resource<User> authorUser,
    FileAccessLevel level
  )
  {
    byte[] encryptedAesKey = KeyManager.Encrypt(group.Data, file.AesKey);

    Resource<FileAccess> access = ToResource<FileAccess>(
      transaction,
      new()
      {
        Id = ObjectId.GenerateNewId(),

        FileId = file.File.Id,
        AuthorUserId = authorUser.Id,
        TargetEntity = new()
        {
          EntityType = FileAccessTargetEntityType.Group,
          EntityId = group.Id,
        },

        EncryptedAesKey = encryptedAesKey,

        Level = level,
      }
    );

    await access.Save(transaction);

    return new(access) { AesKey = file.AesKey };
  }

  public async Task<FileAccessResult?> FindFileAccess(
    ResourceTransaction transaction,
    Resource<File> file,
    Resource<User> user,
    UnlockedUserAuthentication userAuthentication,
    FileAccessLevel minLevel = FileAccessLevel.Read
  )
  {
    if (
      file.Data.OwnerUserId == userAuthentication.UserAuthentication.Data.UserId
      && file.Data.ParentId == null
    )
    {
      return new(user, UnlockedFile.Unlock(file, userAuthentication), null);
    }

    Resource<AdminAccess>? adminAccess = await Query<AdminAccess>(
        transaction,
        (query) =>
          query.Where(
            (item) =>
              item.UserId == userAuthentication.UserAuthentication.Data.UserId
          )
      )
      .FirstOrDefaultAsync(transaction.CancellationToken);

    if (adminAccess != null)
    {
      UnlockedAdminAccess unlockedAdminAccess = UnlockedAdminAccess.Unlock(
        adminAccess,
        userAuthentication
      );

      return new(
        user,
        UnlockedFile.WithAesKey(
          file,
          KeyManager.Decrypt(
            unlockedAdminAccess.AdminAesKey,
            file.Data.AdminEncryptedAesKey
          )
        ),
        null
      );
    }

    Resource<FileAccess>? access = await Query<FileAccess>(
        transaction,
        (query) =>
          query
            .Where(
              (item) =>
                item.FileId == file.Id
                && item.Level >= minLevel
                && item.AuthorUserId
                  == userAuthentication.UserAuthentication.Data.UserId
            )
            .OrderByDescending((item) => item.Level)
      )
      .FirstOrDefaultAsync(transaction.CancellationToken);

    if (access != null)
    {
      if (access.Data.TargetEntity == null)
      {
        return new(
          user,
          UnlockedFile.WithAesKey(file, access.Data.EncryptedAesKey),
          access
        );
      }

      return new(
        user,
        UnlockedFile.WithAesKey(
          file,
          UnlockedFileAccess.Unlock(access, userAuthentication)
        ),
        access
      );
    }

    Resource<File>? parentFolder =
      file.Data.ParentId != null
        ? await Query<File>(
            transaction,
            (query) => query.Where((item) => item.Id == file.Data.ParentId)
          )
          .FirstOrDefaultAsync(transaction.CancellationToken)
        : null;

    if (parentFolder == null)
    {
      return null;
    }

    FileAccessResult? result = await FindFileAccess(
      transaction,
      parentFolder,
      user,
      userAuthentication,
      minLevel
    );

    if (result == null)
    {
      return null;
    }

    return new(
      user,
      UnlockedFile.WithAesKey(
        file,
        KeyManager.Decrypt(result.File.AesKey, file.Data.EncryptedAesKey)
      ),
      result.FileAccess
    );
  }
}

public sealed record FileAccessResult(
  Resource<User> User,
  UnlockedFile File,
  Resource<FileAccess>? FileAccess
)
{
  public FileAccessLevel AccessLevel =>
    File.File.Data.OwnerUserId == User.Id
      ? FileAccessLevel.Full
      : FileAccess?.Data.Level ?? FileAccessLevel.None;
}
