using System;
using System.Linq;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

using Resources;

public sealed partial class Connection
{
  private sealed record class CreateAdminRequest()
  {
    [BsonElement("username")]
    public required string Username;

    [BsonElement("password")]
    public required string Password;

    [BsonElement("confirmPassword")]
    public required string ConfirmPassword;

    [BsonElement("lastName")]
    public required string LastName;

    [BsonElement("firstName")]
    public required string FirstName;

    [BsonElement("middleName")]
    public required string? MiddleName;

    [BsonElement("displayName")]
    public required string? DisplayName;
  };

  private sealed record class CreateAdminResponse() { };

  private TransactedRequestHandler<CreateAdminRequest, CreateAdminResponse> CreateAdmin =>
    async (transaction, request) =>
    {
      if (await Resources.Query<AdminAccess>(transaction).AnyAsync(transaction.CancellationToken))
      {
        throw new InvalidOperationException("Admin user already exists.");
      }

      UsernameValidationFlags usernameValidation = Resources.ValidateUsername(request.Username);

      if (usernameValidation != UsernameValidationFlags.OK)
      {
        throw new InvalidOperationException($"Invalid Username: {usernameValidation}");
      }

      PasswordValidationFlags passwordVerification = Resources.ValidatePassword(request.Password);

      if (passwordVerification != PasswordValidationFlags.OK)
      {
        throw new InvalidOperationException($"Invalid Password: {passwordVerification}");
      }

      await Resources.CreateUser(
        transaction,
        request.Username,
        request.Username,
        request.MiddleName,
        request.LastName,
        request.DisplayName,
        request.Password
      );

      return new();
    };
}
