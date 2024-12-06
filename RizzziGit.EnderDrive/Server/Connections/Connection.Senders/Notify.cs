using System.Threading.Tasks;
using MongoDB.Bson;

namespace RizzziGit.EnderDrive.Server.Connections;

using Resources;

public sealed partial class Connection
{
  public sealed record NotifyRequest
  {
    public required ObjectId NotificationId;
  }

  public sealed record NotifyResponse { }

  public Task<NotifyResponse> Notify(Resource<Notification> notification) =>
    SendRequest<NotifyRequest, NotifyResponse>(
      nameof(Notify),
      new() { NotificationId = notification.Id }
    );
}
