using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using MongoDB.Bson;
using Newtonsoft.Json;

namespace RizzziGit.EnderDrive.Server.Resources;

using MongoDB.Driver;
using Services;

public enum FileType
{
    File,
    Folder,
}

public record class File : ResourceData
{
    public required ObjectId? ParentId;
    public required ObjectId OwnerUserId;

    public required string Name;
    public required FileType Type;

    [JsonIgnore]
    public required byte[] EncryptedAesKey;

    [JsonIgnore]
    public required byte[] AesIv;

    public UnlockedFile Unlock(UnlockedUserAuthentication userAuthentication)
    {
        byte[] aesKey = KeyManager.Decrypt(userAuthentication, EncryptedAesKey);

        return new()
        {
            Original = this,

            Id = Id,
            ParentId = ParentId,
            OwnerUserId = userAuthentication.UserId,

            Name = Name,
            Type = Type,

            EncryptedAesKey = EncryptedAesKey,
            AesIv = AesIv,

            AesKey = aesKey,
        };
    }
}

public record class UnlockedFile : File
{
    public static implicit operator Aes(UnlockedFile file) =>
        KeyManager.DeserializeSymmetricKey([.. file.AesKey, .. file.AesIv]);

    public required File Original;

    public required byte[] AesKey;
}

public sealed partial class ResourceManager
{
    public const int FILE_BUFFER_SIZE = 1024 * 256;

    public async Task<File> GetUserRootFile(ResourceTransaction transaction, User user)
    {
        File? file = await Query<File>(
                transaction,
                (query) => query.Where((item) => item.Id == user.RootFileId)
            )
            .FirstOrDefaultAsync(transaction.CancellationToken);

        if (file == null)
        {
            file = (await CreateFile(transaction, user, file, FileType.Folder, ".ROOT")).Original;

            await UpdateReturn(
                transaction,
                user,
                Builders<User>.Update.Set((item) => item.RootFileId, file.Id)
            );

            user.TrashFileId = file.Id;
        }

        return file;
    }

    public async Task<File> GetUserRootTrash(ResourceTransaction transaction, User user)
    {
        File? file = await Query<File>(
                transaction,
                (query) => query.Where((item) => item.Id == user.TrashFileId)
            )
            .FirstOrDefaultAsync(transaction.CancellationToken);

        if (file == null)
        {
            file = (await CreateFile(transaction, user, file, FileType.Folder, ".TRASH")).Original;

            await UpdateReturn(
                transaction,
                user,
                Builders<User>.Update.Set((item) => item.TrashFileId, file.Id)
            );

            user.TrashFileId = file.Id;
        }

        return file;
    }

    public async Task<UnlockedFile> CreateFile(
        ResourceTransaction transaction,
        User user,
        File? parent,
        FileType type,
        string name
    )
    {
        using Aes aes = await KeyManager.GenerateSymmetricKey(transaction.CancellationToken);

        byte[] aesIv = aes.IV;
        byte[] aesKey = aes.Key;
        byte[] encryptedAesKey = KeyManager.Encrypt(user, aesKey);

        File file =
            new()
            {
                Id = ObjectId.GenerateNewId(),
                ParentId = parent?.Id,
                OwnerUserId = user.Id,

                Name = name,
                Type = type,

                EncryptedAesKey = encryptedAesKey,
                AesIv = aesIv,
            };

        await Insert(transaction, [file]);

        UnlockedFile unlockedFile =
            new()
            {
                Original = file,

                Id = file.Id,
                ParentId = file.ParentId,
                OwnerUserId = user.Id,

                Name = file.Name,
                Type = file.Type,

                EncryptedAesKey = encryptedAesKey,
                AesIv = aesIv,

                AesKey = aesKey,
            };

        return unlockedFile;
    }

    public async Task DeleteFile(ResourceTransaction transaction, UnlockedFile file)
    {
        await foreach (
            FileContent content in Query<FileContent>(
                transaction,
                (query) => query.Where((item) => item.FileId == file.Id)
            )
        )
        {
            await Delete(transaction, content);
        }

        await foreach (
            FileSnapshot snapshot in Query<FileSnapshot>(
                transaction,
                (query) => query.Where((item) => item.FileId == file.Id)
            )
        )
        {
            await DeleteSnapshot(transaction, snapshot);
        }

        await foreach (
            FileData fileData in Query<FileData>(
                transaction,
                (query) => query.Where((item) => item.FileId == file.Id)
            )
        )
        {
            await Delete(transaction, fileData);
        }

        await foreach (
            FileBuffer fileData in Query<FileBuffer>(
                transaction,
                (query) => query.Where((item) => item.FileId == file.Id)
            )
        )
        {
            await Delete(transaction, fileData);
        }

        await foreach (
            FileAccess fileAccess in Query<FileAccess>(
                transaction,
                (query) => query.Where((item) => item.FileId == file.Id)
            )
        )
        {
            await Delete(transaction, fileAccess);
        }

        await Delete(transaction, file.Original);
    }

    public IAsyncEnumerable<File> GetFiles(
        ResourceTransaction transaction,
        File? parentFolder = null,
        FileType? type = null,
        string? name = null,
        User? user = null
    ) =>
        Query<File>(
            transaction,
            (query) =>
                query.Where(
                    (item) =>
                        (parentFolder == null || parentFolder.Id == item.ParentId)
                        && (type == null || item.Type == type)
                        && (
                            name == null
                            || item.Name.Contains(
                                name,
                                System.StringComparison.CurrentCultureIgnoreCase
                            )
                        )
                        && (user == null || user.Id == item.OwnerUserId)
                )
        );
}
