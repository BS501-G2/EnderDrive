using System;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace RizzziGit.EnderDrive.Server.Connections;

using Resources;

public sealed partial class Connection
{
  private UnlockedUserAuthentication Internal_EnsureAuthentication()
  {
    UnlockedUserAuthentication unlockedUserAuthentication =
      GetContext().CurrentUser
      ?? throw new ConnectionResponseException(
        ResponseCode.AuthenticationRequired,
        new ConnectionResponseExceptionData.AuthenticationRequired()
      );

    return unlockedUserAuthentication;
  }

  private async Task<User> Internal_Me(
    ResourceTransaction transaction,
    UserAuthentication userAuthentication
  ) =>
    await Resources
      .GetUsers(
        transaction,
        id: userAuthentication.UserId
      )
      .ToAsyncEnumerable()
      .FirstAsync(
        transaction.CancellationToken
      );

  private static async Task<T> Internal_GetFirst<T>(
    ResourceTransaction transaction,
    IQueryable<T> query
  )
    where T : ResourceData =>
    Internal_EnsureExists(
      await query
        .ToAsyncEnumerable()
        .FirstOrDefaultAsync(
          transaction.CancellationToken
        )
    );

  private static async Task<T> Internal_EnsureFirst<T>(
    ResourceTransaction transaction,
    IQueryable<T> query
  )
    where T : ResourceData =>
    Internal_EnsureExists(
      await Internal_GetFirst(
        transaction,
        query
      )
    );

  private static T Internal_EnsureExists<T>(
    T? item
  )
    where T : ResourceData =>
    item
    ?? throw new ConnectionResponseException(
      ResponseCode.ResourceNotFound,
      new ConnectionResponseExceptionData.ResourceNotFound()
      {
        ResourceName =
          typeof(T).Name,
      }
    );

  private async Task<File> Internal_GetFile(
    ResourceTransaction transaction,
    User me,
    ObjectId? fileId
  ) =>
    Internal_EnsureExists(
      fileId
      != null
        ? await Resources
          .GetFiles(
            transaction,
            id: fileId
          )
          .ToAsyncEnumerable()
          .FirstOrDefaultAsync(
            transaction.CancellationToken
          )
        : await Resources.GetRootFolder(
          transaction,
          me
        )
    );

  private async Task<FileAccessResult> Internal_UnlockFile(
    ResourceTransaction transaction,
    File file,
    User user,
    UnlockedUserAuthentication userAuthentication,
    FileAccessLevel? fileAccessLevel =
      null
  ) =>
    await Resources.FindFileAccess(
      transaction,
      file,
      user,
      userAuthentication,
      fileAccessLevel
        ?? FileAccessLevel.Read
    )
    ?? throw new ConnectionResponseException(
      ResponseCode.Forbidden,
      new ConnectionResponseExceptionData.Forbidden()
      {
        FileId =
          file.Id,
      }
    );
}
