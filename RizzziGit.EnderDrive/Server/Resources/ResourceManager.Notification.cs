using System.Threading.Tasks;
using MongoDB.Bson;

namespace RizzziGit.EnderDrive.Server.Resources;

public sealed record class Notification : ResourceData
{
  public required ObjectId TargetUserId;
}

public sealed partial class ResourceManager
{

  // public async Task<Notification> PushNotification() {}
}
