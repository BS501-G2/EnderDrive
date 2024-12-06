using System;
using System.Linq;
using MongoDB.Bson.Serialization.Attributes;
using RizzziGit.EnderDrive.Server.Resources;

namespace RizzziGit.EnderDrive.Server.Connections;

public sealed partial class Connection
{
  private sealed record class UpdateUsernameRequest
  {
    public required string NewUsername;
  }

  private sealed record class UpdateUsernameResponse { }

  private AuthenticatedRequestHandler<
    UpdateUsernameRequest,
    UpdateUsernameResponse
  > UpdateUsername =>
    async (transaction, request, userAuthentication, me, myAdminAccess) =>
    {
      UsernameValidationFlags flags = Resources.ValidateUsername(request.NewUsername);

      if (flags != UsernameValidationFlags.OK)
      {
        throw new InvalidOperationException($"Invalid username: {flags}");
      }
      else if (
        await Resources
          .Query<User>(
            transaction,
            (query) =>
              query.Where(
                (user) =>
                  user.Username.Equals(
                    request.NewUsername,
                    StringComparison.CurrentCultureIgnoreCase
                  )
              )
          )
          .AnyAsync(transaction)
      )
      {
        throw new InvalidOperationException("Username has already been taken.");
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
