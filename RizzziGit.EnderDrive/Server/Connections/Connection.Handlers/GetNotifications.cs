using MongoDB.Bson;

namespace RizzziGit.EnderDrive.Server.Connections;

using System.Linq;
using Resources;
using Utilities;

public sealed partial class Connection
{
  private sealed record GetNotificationsRequest
  {
    public required bool ExcludeUnread;
    public required bool ExcludeRead;
    public required ObjectId? NotificationId;

    public required PaginationOptions? Pagination;
  }

  private sealed record GetNotificationsResponse
  {
    public required string[] Notifications;
  }

  private AuthenticatedRequestHandler<
    GetNotificationsRequest,
    GetNotificationsResponse
  > GetNotifications =>
    async (transaction, request, userAuthentication, me, myAdminAccess) =>
    {
      Resource<Notification>[] notifications = await Resources
        .Query<Notification>(
          transaction,
          (query) =>
            query
              .Where(
                (notification) =>
                  notification.TargetUserId == me.Id
                  && (request.NotificationId == null || notification.Id == request.NotificationId)
                  && (!request.ExcludeRead || !notification.Read)
                  && (!request.ExcludeUnread || notification.Read)
              )
              .ApplyPagination(request.Pagination)
        )
        .ToArrayAsync(transaction);

      return new() { Notifications = [.. notifications.ToJson()] };
    };
}
