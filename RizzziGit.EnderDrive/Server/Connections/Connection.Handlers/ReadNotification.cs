using System.Linq;
using MongoDB.Bson;
using RizzziGit.EnderDrive.Server.Resources;

namespace RizzziGit.EnderDrive.Server.Connections;

public sealed partial class Connection
{
  private sealed record ReadNotificationRequest
  {
    public required ObjectId NotificationId;
  }

  private sealed record ReadNotificationResponse { }

  private AuthenticatedRequestHandler<
    ReadNotificationRequest,
    ReadNotificationResponse
  > ReadNotification =>
    async (transaction, request, userAuthentication, me, myAdminAccess) =>
    {
      Resource<Notification> notification = await Internal_EnsureFirst(
        transaction,
        Resources.Query<Notification>(
          transaction,
          (query) => query.Where((notification) => request.NotificationId == notification.Id)
        )
      );

      await notification.Update(
        (notification) =>
        {
          notification.Read = true;
        },
        transaction
      );

      return new() { };
    };
}
