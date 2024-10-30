using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace RizzziGit.EnderDrive.Server.Resources;

public sealed record class UserAdminAccess : ResourceData
{
    public required ulong UserId;
    public required ulong AdminUserId;

    [BsonIgnore]
    [JsonIgnore]
    public required byte[] EncryptedPrivateKey;
}

public sealed partial class ResourceManager { }
