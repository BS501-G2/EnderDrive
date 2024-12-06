namespace RizzziGit.EnderDrive.Server.Connections;

using System;
using System.Linq;
using Commons.Memory;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Resources;

public sealed partial class Connection
{
  private sealed record class CreateUserRequest
  {
    public required string Username;
    public required string FirstName;
    public required string? MiddleName;
    public required string LastName;
    public required string? DisplayName;
    public required string? Password;
  }

  private sealed record class CreateUserResponse
  {
    public required ObjectId UserId;
    public required string Password;
  }

  private AdminRequestHandler<CreateUserRequest, CreateUserResponse> CreateUser =>
    async (transaction, request, userAuthentication, me, myAdminAccess) =>
    {
      UsernameValidationFlags flags = Resources.ValidateUsername(request.Username);

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
                  user.Username.Equals(request.Username, StringComparison.CurrentCultureIgnoreCase)
              )
          )
          .AnyAsync(transaction)
      )
      {
        throw new InvalidOperationException("Username has already been taken.");
      }

      string password;

      if (request.Password == null)
      {
        password = ";";
        password += CompositeBuffer.Random(1)[0];
        password += CompositeBuffer.Random(3).ToBase64String().ToLower();
        password += CompositeBuffer.Random(3).ToBase64String().ToUpper();
      }
      else
      {
        password = request.Password;
      }

      (Resource<User> user, _) = await Resources.CreateUser(
        transaction,
        request.Username,
        request.FirstName,
        request.MiddleName,
        request.LastName,
        request.DisplayName,
        password
      );

      return new() { Password = password, UserId = user.Id };
    };
}
