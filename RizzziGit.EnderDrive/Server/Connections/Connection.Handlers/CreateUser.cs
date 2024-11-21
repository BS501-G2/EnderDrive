namespace RizzziGit.EnderDrive.Server.Connections;

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Resources;
using RizzziGit.Commons.Memory;

public sealed partial class Connection
{
  private sealed record class CreateUserRequest
  {
    [BsonElement("username")]
    public required string Username;

    [BsonElement("firstName")]
    public required string FirstName;

    [BsonElement("middleName")]
    public required string? MiddleName;

    [BsonElement("lastName")]
    public required string LastName;

    [BsonElement("displayName")]
    public required string? DisplayName;

    [BsonElement("password")]
    public required string? Password;
  }

  private sealed record class CreateUserResponse
  {
    [BsonElement("userId")]
    public required ObjectId UserId;

    [BsonElement("password")]
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
