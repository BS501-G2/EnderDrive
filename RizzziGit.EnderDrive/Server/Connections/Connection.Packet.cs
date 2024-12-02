using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

[BsonNoId]
public abstract record ConnectionPacket
{
  public sealed record Request : ConnectionPacket
  {
    [BsonElement("name")]
    public required string Name;

    [BsonElement("data")]
    public required byte[] Data;
  }

  public sealed record Response : ConnectionPacket
  {
    [BsonElement("data")]
    public required byte[] Data;
  }

  public sealed record Error : ConnectionPacket
  {
    [BsonElement("message")]
    public required string Message;

    [BsonElement("stack")]
    public required string? Stack;
  }

  private ConnectionPacket() { }

  [BsonElement("id")]
  public required long Id;
}
