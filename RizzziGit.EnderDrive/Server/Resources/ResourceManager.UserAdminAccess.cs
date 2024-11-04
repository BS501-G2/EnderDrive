using Newtonsoft.Json;

namespace RizzziGit.EnderDrive.Server.Resources;

public sealed record class UserAdminAccess : ResourceData
{
    [JsonProperty("userId")]
    public required ulong UserId;

    [JsonProperty("adminUserId")]
    public required ulong AdminUserId;

    [JsonIgnore]
    public required byte[] EncryptedPrivateKey;
}

public sealed partial class ResourceManager { }
