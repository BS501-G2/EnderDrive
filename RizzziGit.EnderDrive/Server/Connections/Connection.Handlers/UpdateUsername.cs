using System;
using MongoDB.Bson.Serialization.Attributes;
using RizzziGit.EnderDrive.Server.Resources;

namespace RizzziGit.EnderDrive.Server.Connections;

public sealed partial class Connection
{
  private sealed record class UpdateUsernameRequest
  {
    [BsonElement("newUsername")]
    public required string NewUsername;
  }

  private sealed record class UpdateUsernameResponse { }

  private AuthenticatedRequestHandler<
    UpdateUsernameRequest,
    UpdateUsernameResponse
  > UpdateUsername =>
    async (transaction, request, userAuthentication, me, myAdminAccess) =>
    {
      if (Resources.ValidateUsername(request.NewUsername) != UsernameValidationFlags.OK)
      {
        throw new InvalidOperationException("Invalid username");
      }

      await me.Update(
        (data) =>
        {
          data.Username = request.NewUsername;
        },
        transaction
      );

      return new() { };
    };
}
