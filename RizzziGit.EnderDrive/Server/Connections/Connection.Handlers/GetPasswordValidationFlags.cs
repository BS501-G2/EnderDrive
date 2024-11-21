using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

using Resources;

public sealed partial class Connection
{
  private sealed record class GetPasswordValidationFlagsRequest
  {
    [BsonElement("password")]
    public required string Password;
  }

  private sealed record class GetPasswordValidationFlagsResponse
  {
    [BsonElement("flags")]
    public required PasswordValidationFlags Flags;
  }

  private RequestHandler<
    GetPasswordValidationFlagsRequest,
    GetPasswordValidationFlagsResponse
  > GetPasswordValidationFlags =>
    async (request, cancellationToken) =>
    {
      return new() { Flags = Resources.ValidatePassword(request.Password) };
    };
}
