using System;
using System.Linq;
using System.Text;
using MongoDB.Bson;

namespace RizzziGit.EnderDrive.Server.Connections;

using Resources;

public sealed partial class Connection
{
  private sealed record class AcceptPasswordResetRequestRequest
  {
    public required ObjectId PasswordResetRequestId;
    public required string Password;
  }

  private sealed record class AcceptPasswordResetRequestResponse { }

  private AdminRequestHandler<
    AcceptPasswordResetRequestRequest,
    AcceptPasswordResetRequestResponse
  > AcceptPasswordResetRequest =>
    async (transaction, request, myUserAuthentication, me, myAdminAccess) =>
    {
      Resource<PasswordResetRequest> passwordResetRequest = await Internal_EnsureFirst(
        transaction,
        Resources.Query<PasswordResetRequest>(
          transaction,
          (query) => query.Where((item) => item.Id == request.PasswordResetRequestId)
        )
      );

      Resource<User> user = await Internal_EnsureFirst(
        transaction,
        Resources.Query<User>(
          transaction,
          (query) => query.Where((item) => item.Id == passwordResetRequest.Data.UserId)
        )
      );

      Resource<UserAuthentication> userAuthentication = await Internal_EnsureFirst(
        transaction,
        Resources.Query<UserAuthentication>(
          transaction,
          (query) =>
            query.Where(
              (userAuthentication) =>
                userAuthentication.UserId == passwordResetRequest.Data.UserId
                && userAuthentication.Type == UserAuthenticationType.Password
            )
        )
      );

      Resource<UserAdminBackdoor> userAdminBackdoor = await Internal_EnsureFirst(
        transaction,
        Resources.Query<UserAdminBackdoor>(
          transaction,
          (query) =>
            query.Where(
              (userAdminBackdoor) => userAdminBackdoor.UserId == passwordResetRequest.Data.UserId
            )
        )
      );

      UnlockedUserAdminBackdoor unlockedUserAdminBackdoor = UnlockedUserAdminBackdoor.Unlock(
        userAdminBackdoor,
        myAdminAccess
      );

      if (passwordResetRequest.Data.Status != PasswordResetRequestStatus.Pending)
      {
        throw new InvalidOperationException("Specified password reset request is not pending.");
      }

      passwordResetRequest.Data.Status = PasswordResetRequestStatus.Accepted;

      await Resources.AddUserAuthentication(
        transaction,
        user,
        unlockedUserAdminBackdoor,
        UserAuthenticationType.Password,
        Encoding.UTF8.GetBytes(request.Password)
      );
      await Resources.Delete(transaction, userAdminBackdoor);
      await passwordResetRequest.Save(transaction);

      return new() { };
    };
}
