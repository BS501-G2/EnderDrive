using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RizzziGit.EnderDrive.Server.Services;

using Commons.Collections;
using Commons.Result;
using Commons.Services;
using Connections;
using Core;
using Resources;
using RizzziGit.Commons.Tasks;

public sealed record NotificationManagerContext
{
  public required WaitQueue<NotificationPushRequest> WaitQueue;
}

public sealed record NotificationPushRequest(
  TaskCompletionSource Source,
  Resource<Notification> Notification
);

public sealed partial class NotificationManager(EnderDriveServer server)
  : Service<NotificationManagerContext>("Notification Manager", server)
{
  public ResourceManager Resources => server.Resources;
  public ConnectionManager Connections => server.Connections;

  public Task CreateAndPush(
    ResourceTransaction transaction,
    Resource<User> actorUser,
    Resource<User> targetUser,
    NotificationData data
  ) =>
    Resources.CreateNotification(transaction, actorUser, targetUser, data).Then(PushNotification);

  public async Task PushNotification(Resource<Notification> notification)
  {
    NotificationManagerContext context = GetContext();
    TaskCompletionSource source = new();

    await context.WaitQueue.Enqueue(new(source, notification));
    await source.Task;
  }

  protected override async Task<NotificationManagerContext> OnStart(
    CancellationToken startupCancellationToken,
    CancellationToken serviceCancellationToken
  )
  {
    return new() { WaitQueue = new() };
  }

  protected override async Task OnRun(
    NotificationManagerContext context,
    CancellationToken serviceCancellationToken
  )
  {
    await foreach (
      (
        TaskCompletionSource source,
        Resource<Notification> notification
      ) in context.WaitQueue.WithCancellation(serviceCancellationToken)
    )
    {
      try
      {
        foreach (Connection connection in Connections)
        {
          if (
            connection.CurrentUser == null
            || connection.CurrentUser.UserAuthentication.Data.UserId
              != notification.Data.TargetUserId
          )
          {
            continue;
          }

          await PushToConnection(notification, connection);
        }

        source.SetResult();
      }
      catch (Exception exception)
      {
        source.SetException(exception);
        Error(exception);
      }
    }
  }

  private async Task PushToConnection(Resource<Notification> notification, Connection connection)
  {
    _ = Task.Run(async () =>
    {
      if (await connection.IsHandlerAvailable(nameof(connection.Notify)))
      {
        await connection.Notify(notification);
      }
    });
  }
}
