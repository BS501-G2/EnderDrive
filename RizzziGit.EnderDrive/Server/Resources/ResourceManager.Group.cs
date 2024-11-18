using System.Security.Cryptography;
using System.Threading.Tasks;
using MongoDB.Bson;
using Newtonsoft.Json;

namespace RizzziGit.EnderDrive.Server.Resources;

using MongoDB.Bson.Serialization.Attributes;
using Services;

public record class Group : ResourceData
{
  public static implicit operator RSA(Group groupMembership) =>
    KeyManager.DeserializeAsymmetricKey(groupMembership.RsaPublicKey);

  [JsonProperty("name")]
  public required string Name;

  [JsonProperty("description")]
  public required string? Description;

  [JsonIgnore]
  public required byte[] RsaPublicKey;
}

public enum GroupMembershipRole
{
  Member,
  Admin,
  Owner,
}

public record class GroupMembership : ResourceData
{
  public static implicit operator RSA(GroupMembership groupMembership) =>
    KeyManager.DeserializeAsymmetricKey(groupMembership.RsaPublicKey);

  [BsonElement("groupId")]
  public required ObjectId GroupId;

  [BsonIgnore]
  public required ObjectId UserId;

  [BsonIgnore]
  public required byte[] RsaPublicKey;

  [BsonIgnore]
  public required byte[] EncryptedRsaPrivateKey;

  [BsonElement("role")]
  public required GroupMembershipRole Role;

  [BsonElement("accepted")]
  public required bool Accepted;
}

public sealed partial class ResourceManager
{
  public async Task<(Resource<Group> Group, UnlockedGroupMembership GroupMembership)> CreateGroup(
    ResourceTransaction transaction,
    string name,
    string? description,
    UnlockedUserAuthentication unlockedUserAuthentication
  )
  {
    RSA rsa = await KeyManager.GenerateAsymmetricKey(transaction.CancellationToken);
    byte[] rsaPrivateKey = KeyManager.SerializeAsymmetricKey(rsa, true);
    byte[] rsaPublicKey = KeyManager.SerializeAsymmetricKey(rsa, false);

    Resource<Group> group = ToResource<Group>(
      transaction,
      new()
      {
        Id = ObjectId.Empty,

        Name = name,
        Description = description,
        RsaPublicKey = rsaPublicKey,
      }
    );

    await group.Save(transaction);

    return (
      group,
      await AddInitialGroupMember(
        transaction,
        group,
        unlockedUserAuthentication,
        rsaPrivateKey,
        rsaPublicKey
      )
    );
  }

  private async Task<UnlockedGroupMembership> AddInitialGroupMember(
    ResourceTransaction transaction,
    Resource<Group> group,
    UnlockedUserAuthentication userAuthentication,
    byte[] rsaPrivateKey,
    byte[] rsaPublicKey
  )
  {
    byte[] encryptedRsaPrivateKey = KeyManager.Encrypt(userAuthentication, rsaPrivateKey);

    Resource<GroupMembership> membership = ToResource<GroupMembership>(
      transaction,
      new()
      {
        Id = ObjectId.Empty,

        GroupId = group.Id,
        UserId = userAuthentication.UserAuthentication.Data.UserId,

        RsaPublicKey = rsaPublicKey,
        EncryptedRsaPrivateKey = encryptedRsaPrivateKey,

        Role = GroupMembershipRole.Owner,
        Accepted = true,
      }
    );

    await membership.Save(transaction);
    return new(membership) { RsaPrivateKey = rsaPrivateKey };
  }

  public async Task<UnlockedGroupMembership> AddGroupMember(
    ResourceTransaction transaction,
    UnlockedGroupMembership adder,
    Resource<User> user
  )
  {
    byte[] encryptedRsaPublicKey = KeyManager.Encrypt(user.Data, adder.RsaPrivateKey);

    Resource<GroupMembership> membership = ToResource<GroupMembership>(
      transaction,
      new()
      {
        Id = ObjectId.Empty,

        GroupId = adder.GroupMembership.Data.GroupId,
        UserId = user.Data.Id,

        RsaPublicKey = adder.GroupMembership.Data.RsaPublicKey,
        EncryptedRsaPrivateKey = encryptedRsaPublicKey,

        Role = GroupMembershipRole.Member,
        Accepted = false,
      }
    );

    await membership.Save(transaction);
    return new(membership) { RsaPrivateKey = adder.RsaPrivateKey };
  }
}
