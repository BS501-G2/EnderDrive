using System;
using System.Text;
using MongoDB.Bson.Serialization.Attributes;
using RizzziGit.Commons.Memory;
using RizzziGit.EnderDrive.Server.Resources;

namespace RizzziGit.EnderDrive.Server.Connections;

public sealed partial class Connection
{
  private sealed partial class UpdatePasswordRequest
  {
    [BsonElement("current-password")]
    public required string CurrentPassword;

    [BsonElement("newPassword")]
    public required string NewPassword;

    [BsonElement("confirm-password")]
    public required string ConfirmPassword;
  }

  private sealed partial class UpdatePasswordResponse { }

  private AuthenticatedRequestHandler<
    UpdatePasswordRequest,
    UpdatePasswordResponse
  > UpdatePassword =>
    async (transaction, request, userAuthentication, me, myAdminAccess) =>
    {
      PasswordValidationFlags flags = Resources.ValidatePassword(
        request.NewPassword,
        request.ConfirmPassword
      );

      if (flags != PasswordValidationFlags.OK)
      {
        throw new InvalidOperationException($"Invalid password: {flags}");
      }

      await Resources.AddUserAuthentication(
        transaction,
        me,
        userAuthentication,
        UserAuthenticationType.Password,
        Encoding.UTF8.GetBytes(request.NewPassword)
      );

      return new() { };
    };
}
