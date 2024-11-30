using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Newtonsoft.Json;

namespace RizzziGit.EnderDrive.Server.Resources;

using Resources;
using Services;

public enum FileType
{
  File,
  Folder,
}

public enum TrashOptions
{
  NonInclusive,
  Inclusive,
  Exclusive,
}

[Flags]
public enum FileNameValidationFlags
{
  OK = 0,

  TooLong = 1 << 0,
  TooShort = 1 << 1,
  InvalidCharacters = 1 << 2,
  FileExists = 1 << 3,
}

public record class File : ResourceData
{
  public required ObjectId? ParentId;
  public required ObjectId OwnerUserId;
  public required string Name;
  public required FileType Type;
  public required long? TrashTime;

  [JsonIgnore]
  public required byte[] EncryptedAesKey;

  [JsonIgnore]
  public required byte[] AdminEncryptedAesKey;
}

public sealed partial class ResourceManager
{
  public const int FILE_BUFFER_SIZE = 1024 * 256;

  public async ValueTask<FileNameValidationFlags> ValidateFileName(
    ResourceTransaction transaction,
    string name,
    Resource<File>? parnet
  )
  {
    FileNameValidationFlags flags = 0;

    if (name.Length < 1)
    {
      flags |= FileNameValidationFlags.TooShort;
    }

    if (name.Length > 100)
    {
      flags |= FileNameValidationFlags.TooLong;
    }

    if (
      parnet != null
      && await Query<File>(
          transaction,
          (query) =>
            query.Where(
              (file) =>
                file.ParentId == parnet.Id
                && file.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase)
            )
        )
        .AnyAsync(transaction.CancellationToken)
    )
    {
      flags |= FileNameValidationFlags.FileExists;
    }

    return flags;
  }

  public async Task<UnlockedFile> GetRootFolder(
    ResourceTransaction transaction,
    Resource<User> user,
    UnlockedUserAuthentication userAuthentication
  )
  {
    Resource<File>? file = await Query<File>(
        transaction,
        (query) =>
          query.Where((item) => item.OwnerUserId == user.Id && item.Id == user.Data.RootFileId)
      )
      .FirstOrDefaultAsync(transaction);

    if (file != null)
    {
      return UnlockedFile.Unlock(file, userAuthentication);
    }

    byte[] aesKey = RandomNumberGenerator.GetBytes(32);
    byte[] encryptedAesKey = KeyManager.Encrypt(user.Data, aesKey);
    byte[] adminEcnryptedAesKey = KeyManager.Encrypt(AdminManager.AdminKey, aesKey);

    file = ToResource<File>(
      transaction,
      new()
      {
        Id = ObjectId.Empty,
        ParentId = null,
        OwnerUserId = user.Id,

        Name = ".ROOT",
        Type = FileType.Folder,

        EncryptedAesKey = encryptedAesKey,
        AdminEncryptedAesKey = adminEcnryptedAesKey,

        TrashTime = null,
      }
    );

    await file.Save(transaction);
    user.Data.RootFileId = file.Id;
    await user.Save(transaction);

    return new(file) { AesKey = aesKey };
  }

  public async Task<UnlockedFile> GetTrashFolder(
    ResourceTransaction transaction,
    Resource<User> user,
    UnlockedUserAuthentication userAuthentication
  )
  {
    Resource<File>? file = await Query<File>(
        transaction,
        (query) =>
          query.Where((item) => item.OwnerUserId == user.Id && item.Id == user.Data.TrashFileId)
      )
      .FirstOrDefaultAsync(transaction);

    if (file != null)
    {
      return UnlockedFile.Unlock(file, userAuthentication);
    }

    byte[] aesKey = RandomNumberGenerator.GetBytes(32);
    byte[] encryptedAesKey = KeyManager.Encrypt(user.Data, aesKey);
    byte[] adminEcnryptedAesKey = KeyManager.Encrypt(AdminManager.AdminKey, aesKey);

    file = ToResource<File>(
      transaction,
      new()
      {
        Id = ObjectId.Empty,
        ParentId = null,
        OwnerUserId = user.Id,

        Name = ".TRASH",
        Type = FileType.Folder,

        EncryptedAesKey = encryptedAesKey,
        AdminEncryptedAesKey = adminEcnryptedAesKey,

        TrashTime = null,
      }
    );

    await file.Save(transaction);
    user.Data.TrashFileId = file.Id;
    await user.Save(transaction);

    return new(file) { AesKey = aesKey };
  }

  public async Task<UnlockedFile> CreateFile(
    ResourceTransaction transaction,
    UnlockedFile parent,
    FileType type,
    string name
  )
  {
    byte[] aesKey = RandomNumberGenerator.GetBytes(32);
    byte[] encryptedAesKey = KeyManager.Encrypt(parent, aesKey);
    byte[] adminEcnryptedAesKey = KeyManager.Encrypt(AdminManager.AdminKey, aesKey);

    Resource<File> file = ToResource<File>(
      transaction,
      new()
      {
        Id = ObjectId.Empty,
        ParentId = parent.File.Id,
        OwnerUserId = parent.File.Data.OwnerUserId,

        Name = name,
        Type = type,

        EncryptedAesKey = encryptedAesKey,
        AdminEncryptedAesKey = adminEcnryptedAesKey,

        TrashTime = null,
      }
    );

    await file.Save(transaction);

    return new(file) { AesKey = aesKey };
  }

  public async ValueTask MoveFile(
    ResourceTransaction transaction,
    UnlockedFile file,
    UnlockedFile destinationFolder
  )
  {
    byte[] fileAesKey = file.AesKey;
    byte[] encryptedFileAesKey = KeyManager.Encrypt(destinationFolder, fileAesKey);

    await file.File.Update(
      (file) =>
      {
        file.ParentId = destinationFolder.File.Id;
        file.EncryptedAesKey = encryptedFileAesKey;
        return file;
      },
      transaction
    );
  }

  public async Task Delete(ResourceTransaction transaction, Resource<File> file)
  {
    await foreach (
      Resource<FileAccess> fileAccess in Query<FileAccess>(
        transaction,
        (query) => query.Where((item) => item.FileId == file.Id)
      )
    )
    {
      await Delete(transaction, fileAccess);
    }

    await foreach (
      Resource<FileStar> fileStar in Query<FileStar>(
        transaction,
        (query) => query.Where((item) => item.FileId == file.Id)
      )
    )
    {
      await Delete(transaction, fileStar);
    }

    await Delete<File>(transaction, file);
  }

  internal bool TryGetActiveFileStream(object streamId, out FileResourceStream? stream)
  {
    throw new NotImplementedException();
  }
}

public static class IQueryableExtensions
{
  public static IQueryable<T> ApplyFilter<T>(
    this IQueryable<T> query,
    Func<IQueryable<T>, IQueryable<T>>? onQuery
  )
    where T : ResourceData
  {
    if (onQuery != null)
    {
      return onQuery(query);
    }

    return query;
  }
}
