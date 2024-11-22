using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using RizzziGit.EnderDrive.Server.Resources;

namespace RizzziGit.EnderDrive.Server.Connections;

public sealed partial class Connection
{
  private sealed record class DeclinePasswordResetRequestRequest
  {
    [BsonElement("passwordResetRequestId")]
    public required ObjectId PasswordResetRequestId;
  }

  private sealed record class DeclinePasswordResetRequestResponse { }

  private AdminRequestHandler<
    DeclinePasswordResetRequestRequest,
    DeclinePasswordResetRequestResponse
  > DeclinePasswordResetRequest =>
    async (transaction, request, userAuthentication, me, myAdminAccess) =>
    {
      Resource<PasswordResetRequest> passwordResetRequest = await Internal_EnsureFirst(
        transaction,
        Resources.Query<PasswordResetRequest>(
          transaction,
          (query) =>
            query.Where(
              (passwordResetRequest) => passwordResetRequest.Id == request.PasswordResetRequestId
            )
        )
      );

      if (passwordResetRequest.Data.Status != PasswordResetRequestStatus.Pending)
      {
        throw new ConnectionResponseException(
          ResponseCode.InvalidOperation,
          new ConnectionResponseExceptionData.InvalidOperation() { },
          "Password reset request is not pending"
        );
      }

      await passwordResetRequest.Update(
        (data) =>
        {
          data.Status = PasswordResetRequestStatus.Declined;
        },
        transaction
      );

      return new() { };
    };
}
