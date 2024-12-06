using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace RizzziGit.EnderDrive.Server.Connections;

using System;
using Resources;

public sealed partial class Connection
{
  private ValueTask<Resource<User>?> Internal_Me(
    ResourceTransaction transaction,
    UnlockedUserAuthentication userAuthentication
  ) =>
    Resources
      .Query<User>(
        transaction,
        (query) =>
          query.Where((item) => item.Id == userAuthentication.UserAuthentication.Data.UserId)
      )
      .FirstOrDefaultAsync();

  private static ValueTask<Resource<T>?> Internal_GetFirst<T>(
    ResourceTransaction transaction,
    IAsyncEnumerable<Resource<T>> query
  )
    where T : ResourceData => query.FirstOrDefaultAsync(transaction.CancellationToken);

  private static async Task<Resource<T>> Internal_EnsureFirst<T>(
    ResourceTransaction transaction,
    IAsyncEnumerable<Resource<T>> query
  )
    where T : ResourceData => Internal_EnsureExists(await Internal_GetFirst(transaction, query));

  private Task<Resource<T>> Internal_EnsureFirst<T>(
    ResourceTransaction transaction,
    Func<IQueryable<T>, IQueryable<T>> query
  )
    where T : ResourceData =>
    Internal_EnsureFirst(transaction, Resources.Query<T>(transaction, query));

  private static Resource<T> Internal_EnsureExists<T>(Resource<T>? item)
    where T : ResourceData =>
    item ?? throw new NotFoundException() { ResourceName = typeof(T).Name };

  private async Task<Resource<File>> Internal_GetFile(
    ResourceTransaction transaction,
    Resource<User> me,
    UnlockedUserAuthentication userAuthentication,
    ObjectId? fileId
  ) =>
    Internal_EnsureExists(
      fileId != null
        ? await Resources
          .Query<File>(transaction, (query) => query.Where((item) => item.Id == fileId))
          .FirstOrDefaultAsync(transaction.CancellationToken)
        : (await Resources.GetRootFolder(transaction, me, userAuthentication)).File
    );

  private async Task<FileAccessResult> Internal_UnlockFile(
    ResourceTransaction transaction,
    Resource<File> file,
    Resource<User> user,
    UnlockedUserAuthentication userAuthentication,
    FileAccessLevel? fileAccessLevel = null
  ) =>
    await Resources.FindFileAccess(
      transaction,
      file,
      user,
      userAuthentication,
      fileAccessLevel ?? FileAccessLevel.Read
    ) ?? throw new FileAccessForbiddenException() { FileId = file.Id };

  private async Task Internal_BroadcastFileActivity(
    ResourceTransaction transaction,
    Resource<User> me,
    UnlockedFile file,
    Resource<FileAccess>? baseFileAccess,
    NotificationData data
  )
  {
    await foreach (
      Resource<FileAccess> fileAccess in Resources.Query<FileAccess>(
        transaction,
        (query) =>
          query.Where(
            (fileAccess) =>
              fileAccess.TargetUserId != null
              && fileAccess.TargetUserId != me.Id
              && (baseFileAccess != null ? baseFileAccess.Data.FileId : file.File.Id)
                == fileAccess.FileId
          )
      )
    )
    {
      Resource<User> user = await Internal_EnsureFirst<User>(
        transaction,
        (query) => query.Where((user) => user.Id == fileAccess.Data.TargetUserId)
      );

      await Notifications.CreateAndPush(transaction, me, user, data);
    }

    if (me.Id != file.File.Data.OwnerUserId)
    {
      Resource<User> fileOwner = await Internal_EnsureFirst<User>(
        transaction,
        (query) => query.Where((user) => user.Id == file.File.Data.OwnerUserId)
      );

      await Notifications.CreateAndPush(transaction, me, fileOwner, data);
    }
  }
}
