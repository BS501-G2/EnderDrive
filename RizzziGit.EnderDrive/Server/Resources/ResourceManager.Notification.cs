using System.IO;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Resources;

public sealed record class Notification : ResourceData
{
  public required ObjectId TargetUserId;

  public required BsonDocument Data;
}

[BsonNoId]
public abstract record NotificationData
{
  public static NotificationData Deserialize(BsonDocument document)
  {
    document = document.DeepClone().AsBsonDocument;

    if (!document.TryGetValue("type", out BsonValue type))
    {
      throw new InvalidDataException("Failed to deserialize.");
    }

    document.Remove("type");
    return type.AsString switch
    {
      "shared" => BsonSerializer.Deserialize<SharedFile>(document),
      "updated" => BsonSerializer.Deserialize<UpdatedFile>(document),

      _ => throw new InvalidDataException($"Invalid type: {type.AsString}")
    };
  }

  public static BsonDocument Serialize(NotificationData data)
  {
    BsonDocument document = data.ToBsonDocument();

    document.Add(
      "type",
      data switch
      {
        SharedFile => "shared",
        UpdatedFile => "updated",

        _ => throw new InvalidDataException($"Invalid type: {data.GetType().Name}")
      }
    );

    return document;
  }

  public sealed record SharedFile : NotificationData
  {
    public required ObjectId FileAccessId;
  }

  public sealed record UpdatedFile : NotificationData
  {
    public required ObjectId FileLogId;
  }

  private NotificationData() { }
}

public sealed partial class ResourceManager
{

  // public async Task<Notification> PushNotification() {}
}
