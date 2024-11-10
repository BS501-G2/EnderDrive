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
            GetContext().CurrentUser ?? throw new InvalidOperationException("Not authenticated.");

        return unlockedUserAuthentication;
    }

    private async Task<User> Internal_Me(
        ResourceTransaction transaction,
        UserAuthentication userAuthentication
    ) =>
        await Resources
            .GetUsers(transaction, id: userAuthentication.UserId)
            .ToAsyncEnumerable()
            .FirstAsync(transaction.CancellationToken);

    private static async Task<T> Internal_GetFirst<T>(
        ResourceTransaction transaction,
        IQueryable<T> query
    )
        where T : ResourceData =>
        Internal_EnsureExists(
            await query.ToAsyncEnumerable().FirstOrDefaultAsync(transaction.CancellationToken)
        );

    private static T Internal_EnsureExists<T>(T? item)
        where T : ResourceData =>
        item ?? throw new InvalidOperationException($"Resource item not found");

    private async Task<File> Internal_GetFile(
        ResourceTransaction transaction,
        User me,
        ObjectId? objectId
    ) =>
        Internal_EnsureExists(
            objectId != null
                ? await Resources
                    .GetFiles(transaction, id: objectId)
                    .ToAsyncEnumerable()
                    .FirstOrDefaultAsync(transaction.CancellationToken)
                : await Resources.GetRootFolder(transaction, me)
        );

    private async Task<FileAccessResult> Internal_UnlockFile(
        ResourceTransaction transaction,
        File file,
        User user,
        UnlockedUserAuthentication userAuthentication
    ) =>
        await Resources.FindFileAccess(transaction, file, user, userAuthentication)
        ?? throw new InvalidOperationException("No access to this file");

    private static ObjectId? Internal_ParseId(string? objectId) =>
        objectId != null ? ObjectId.Parse(objectId) : null;
}
