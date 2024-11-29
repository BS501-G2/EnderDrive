namespace RizzziGit.EnderDrive.Server.Connections;

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Resources;
using Commons.Memory;

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
