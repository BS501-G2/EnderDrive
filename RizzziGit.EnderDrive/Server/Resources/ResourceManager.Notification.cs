using System;
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
  public required ObjectId ActorUserId;
  public required NotificationData Data;
  public required long CreateTime;
  public required bool Read;
}

[BsonNoId]
public abstract record NotificationData
{
  public static void BindPolymorphicTypes()
  {
    BsonClassMap.RegisterClassMap<File.FileShare>();
    BsonClassMap.RegisterClassMap<File.FileUnshare>();
    BsonClassMap.RegisterClassMap<File.FileCreate>();
    BsonClassMap.RegisterClassMap<File.FileUpdate>();
    BsonClassMap.RegisterClassMap<File.FileTrash>();
    BsonClassMap.RegisterClassMap<File.FileDelete>();
    BsonClassMap.RegisterClassMap<File.FileRestore>();
    BsonClassMap.RegisterClassMap<File.FileMove>();
    BsonClassMap.RegisterClassMap<File.FileRename>();
  }

  public abstract string Type { get; }

  public abstract record File : NotificationData
  {
    public sealed record FileShare : File
    {
      public required ObjectId FileAccessId;

      public override string Type => "Shared";
    }

    public sealed record FileUnshare : File
    {
      public override string Type => "Unshared";
    }

    public sealed record FileCreate : File
    {
      public override string Type => "Created";
    }

    public sealed record FileUpdate : File
    {
      public required ObjectId FileDataId;
      public override string Type => "Updated";
    }

    public sealed record FileTrash : File
    {
      public override string Type => "Trashed";
    }

    public sealed record FileDelete : File
    {
      public override string Type => "Deleted";
    }

    public sealed record FileMove : File
    {
      public override string Type => "Moved";
    }

    public sealed record FileRename : File
    {
      public override string Type => "Renamed";
    }

    public sealed record FileRestore : File
    {
      public override string Type => "Restored";
    }

    public required ObjectId FileId;
  }
}

public sealed partial class ResourceManager
{
  public async Task<Resource<Notification>> CreateNotification(
    ResourceTransaction transaction,
    Resource<User> actorUser,
    Resource<User> targetUser,
    NotificationData data
  )
  {
    Resource<Notification> notification = ToResource<Notification>(
      transaction,
      new()
      {
        ActorUserId = actorUser.Id,
        TargetUserId = targetUser.Id,
        Data = data,
        Read = false,
        CreateTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
      }
    );

    await notification.Save(transaction);
    return notification;
  }
}
