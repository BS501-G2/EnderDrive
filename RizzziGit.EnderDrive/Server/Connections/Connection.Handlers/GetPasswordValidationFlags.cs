using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

using Resources;

public sealed partial class Connection
{
  private sealed record class GetPasswordValidationFlagsRequest
  {
    public required string Password;
    public required string? ConfirmPassword;
  }

  private sealed record class GetPasswordValidationFlagsResponse
  {
    public required PasswordValidationFlags Flags;
  }

  private RequestHandler<
    GetPasswordValidationFlagsRequest,
    GetPasswordValidationFlagsResponse
  > GetPasswordValidationFlags =>
    async (request, cancellationToken) =>
    {
      return new()
      {
        Flags = Resources.ValidatePassword(request.Password, request.ConfirmPassword),
      };
    };
}
