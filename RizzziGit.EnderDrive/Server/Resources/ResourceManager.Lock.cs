using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace RizzziGit.EnderDrive.Server.Resources;

using System;
using Services;

public abstract record class UnlockedResource<T>(Resource<T> Resource)
  where T : ResourceData
{
  public string ToJson() => Resource.ToJson();
}

public sealed record class UnlockedUserAuthentication(
  Resource<UserAuthentication> UserAuthentication
) : UnlockedResource<UserAuthentication>(UserAuthentication)
{
  public static UnlockedUserAuthentication Unlock(
    Resource<UserAuthentication> userAuthentication,
    UnlockedUserAuthentication sourceUserAuthentication
  ) => Unlock(userAuthentication, sourceUserAuthentication.Payload);

  public static UnlockedUserAuthentication Unlock(
    Resource<UserAuthentication> userAuthentication,
    string payload
  ) => Unlock(userAuthentication, Encoding.UTF8.GetBytes(payload));

  public static UnlockedUserAuthentication Unlock(
    Resource<UserAuthentication> userAuthentication,
    byte[] payload
  )
  {
    byte[] aesKey;
    {
      using Rfc2898DeriveBytes rfc2898DeriveBytes =
        new(
          payload,
          userAuthentication.Data.Salt,
          userAuthentication.Data.Iterations,
          HashAlgorithmName.SHA256
        );

      aesKey = rfc2898DeriveBytes.GetBytes(32);
    }

    byte[] rsaPrivateKey = KeyManager.Decrypt(
      aesKey,
      userAuthentication.Data.EncryptedUserPrivateRsaKey
    );

    return new(userAuthentication)
    {
      Payload = payload,
      AesKey = payload,
      UserRsaPrivateKey = rsaPrivateKey,
    };
  }

  public static implicit operator RSA(UnlockedUserAuthentication user) =>
    KeyManager.DeserializeAsymmetricKey(user.UserRsaPrivateKey);

  public required byte[] Payload;
  public required byte[] AesKey;
  public required byte[] UserRsaPrivateKey;
}

public record class UnlockedAdminAccess(Resource<AdminAccess> AdminAccess)
  : UnlockedResource<AdminAccess>(AdminAccess)
{
  public static UnlockedAdminAccess Unlock(
    Resource<AdminAccess> adminAccess,
    UnlockedUserAuthentication userAuthentication
  )
  {
    byte[] aesKey = KeyManager.Decrypt(userAuthentication, adminAccess.Data.EncryptedAesKey);

    return WithAesKey(adminAccess, aesKey);
  }

  public static UnlockedAdminAccess WithAesKey(Resource<AdminAccess> adminAccess, byte[] aesKey)
  {
    return new(adminAccess) { AdminAesKey = aesKey };
  }

  public static implicit operator byte[](UnlockedAdminAccess adminClass) => adminClass.AdminAesKey;

  [JsonIgnore]
  public required byte[] AdminAesKey;
}

public sealed record class UnlockedAdminKey(Resource<AdminKey> AdminKey)
  : UnlockedResource<AdminKey>(AdminKey)
{
  public static UnlockedAdminKey Unlock(Resource<AdminKey> adminKey, string password)
  {
    using Rfc2898DeriveBytes rfc2898DeriveBytes =
      new(
        Encoding.UTF8.GetBytes(password),
        adminKey.Data.Salt,
        adminKey.Data.Iterations,
        HashAlgorithmName.SHA256
      );
    byte[] aesKey = rfc2898DeriveBytes.GetBytes(32);

    byte[] decryptedChallengeBytes = KeyManager.Decrypt(
      aesKey,
      adminKey.Data.EncryptedChallengeBytes
    );

    return new UnlockedAdminKey(adminKey) { AesKey = aesKey };
  }

  public static implicit operator byte[](UnlockedAdminKey adminKey) => adminKey.AesKey;

  public required byte[] AesKey;
}

public sealed record UnlockedFile(Resource<File> File)
{
  public static UnlockedFile WithAesKey(Resource<File> file, byte[] aesKey)
  {
    return new(file) { AesKey = aesKey };
  }

  public static UnlockedFile Unlock(Resource<File> file, UnlockedFile parentFolder)
  {
    if (file.Data.ParentId == null)
    {
      throw new InvalidOperationException("Requires user authentication to decrypt.");
    }

    try
    {
      return WithAesKey(file, KeyManager.Decrypt(parentFolder, file.Data.EncryptedAesKey));
    }
    catch (Exception exception)
    {
      throw new InvalidOperationException("Failed to decrypt", exception);
    }
  }

  public static UnlockedFile Unlock(
    Resource<File> file,
    UnlockedUserAuthentication userAuthentication
  )
  {
    if (file.Data.ParentId != null)
    {
      throw new InvalidOperationException("Requires parent file to decrypt.");
    }

    try
    {
      return WithAesKey(file, KeyManager.Decrypt(userAuthentication, file.Data.EncryptedAesKey));
    }
    catch (Exception exception)
    {
      throw new InvalidOperationException("Failed to decrypt", exception);
    }
  }

  public static implicit operator byte[](UnlockedFile file) => file.AesKey;

  public required byte[] AesKey;
}

public record class UnlockedFileAccess(Resource<FileAccess> FileAccess)
  : UnlockedResource<FileAccess>(FileAccess)
{
  public static UnlockedFileAccess Unlock(
    Resource<FileAccess> fileAccess,
    UnlockedUserAuthentication userAuthentication
  )
  {
    return new(fileAccess)
    {
      AesKey =
        fileAccess.Data.TargetUserId != null
          ? KeyManager.Decrypt(userAuthentication, fileAccess.Data.EncryptedAesKey)
          : fileAccess.Data.EncryptedAesKey,
    };
  }

  public static implicit operator byte[](UnlockedFileAccess file) => file.AesKey;

  public required byte[] AesKey;

  public UnlockedFile UnlockFile(Resource<File> file)
  {
    return UnlockedFile.WithAesKey(file, AesKey);
  }
}

public record class UnlockedGroupMembership(Resource<GroupMembership> GroupMembership)
  : UnlockedResource<GroupMembership>(GroupMembership)
{
  public static UnlockedGroupMembership Unlock(
    Resource<GroupMembership> groupMembership,
    UnlockedUserAuthentication userAuthentication
  )
  {
    byte[] rsaPrivateKey = KeyManager.Decrypt(
      userAuthentication,
      groupMembership.Data.EncryptedRsaPrivateKey
    );

    return new(groupMembership) { RsaPrivateKey = rsaPrivateKey };
  }

  public static implicit operator RSA(UnlockedGroupMembership groupMembership) =>
    KeyManager.DeserializeAsymmetricKey(groupMembership.RsaPrivateKey);

  public required byte[] RsaPrivateKey;
}

public record class UnlockedUserAdminBackdoor(Resource<UserAdminBackdoor> UserAdminBackdoor)
  : UnlockedResource<UserAdminBackdoor>(UserAdminBackdoor)
{
  public static UnlockedUserAdminBackdoor Unlock(
    Resource<UserAdminBackdoor> adminBackdoor,
    UnlockedAdminAccess adminAccess
  )
  {
    return new(adminBackdoor)
    {
      UserPrivateRsaKey = KeyManager.Decrypt(
        adminAccess,
        adminBackdoor.Data.EncryptedUserPrivateRsaKey
      ),
    };
  }

  public static implicit operator byte[](UnlockedUserAdminBackdoor userAdminBackdoor) =>
    userAdminBackdoor.UserPrivateRsaKey;

  public required byte[] UserPrivateRsaKey;
}
