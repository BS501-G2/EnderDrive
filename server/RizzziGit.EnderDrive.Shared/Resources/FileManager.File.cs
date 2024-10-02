using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Shared.Resources;

public enum FileType
{
    File,
    Folder,
}

public class File : ResourceData
{
    public required ObjectId? ParentId;
    public required ObjectId UserId;

    public required string Name;
    public required FileType Type;

    [BsonIgnore]
    public required byte[] EncryptedAesKey;

    [BsonIgnore]
    public required byte[] AesIv;
}
