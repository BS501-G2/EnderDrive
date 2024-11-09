using MongoDB.Bson;

namespace RizzziGit.EnderDrive.Server.Resources;

public sealed record class PasswordResetRequest : ResourceData
{
    public required ObjectId UserId;
}

public sealed partial class ResourceManager { }
