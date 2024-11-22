using System;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace RizzziGit.EnderDrive.Server.Connections;

using System.Collections.Generic;
using System.Threading;
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

  private static Resource<T> Internal_EnsureExists<T>(Resource<T>? item)
    where T : ResourceData =>
    item
    ?? throw new ConnectionResponseException(
      ResponseCode.ResourceNotFound,
      new ConnectionResponseExceptionData.ResourceNotFound() { ResourceName = typeof(T).Name }
    );

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
    )
    ?? throw new ConnectionResponseException(
      ResponseCode.Forbidden,
      new ConnectionResponseExceptionData.Forbidden() { FileId = file.Id }
    );

  private async Task Internal_CloseAllStreams()
  {
    ConnectionContext context = GetContext();

    foreach ((_, ConnectionByteStream stream) in context.FileStreams)
    {
      await stream.Close(CancellationToken.None);
    }
  }
}
