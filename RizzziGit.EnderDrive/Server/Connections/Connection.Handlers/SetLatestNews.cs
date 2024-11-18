using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

public sealed partial class Connection
{
  private sealed record class SetLatestNewsRequest
  {
    [BsonElement("newsId")]
    public required ObjectId NewsId;
  }

  private sealed record class SetLatestNewsResponse
  {
    // public 
  }
}
