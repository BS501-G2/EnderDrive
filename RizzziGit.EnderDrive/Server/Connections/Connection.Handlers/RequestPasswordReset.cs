using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

using System.Linq;
using Resources;

public sealed partial class Connection
{
  private sealed record class RequestPasswordResetRequest
  {
    [BsonElement("userId")]
    public required ObjectId? UserId;

    [BsonElement("username")]
    public required string? Username;
  }

  private sealed record class RequestPasswordResetResponse { }

  private TransactedRequestHandler<
    RequestPasswordResetRequest,
    RequestPasswordResetResponse
  > RequestPasswordReset =>
    async (transaction, request) =>
    {
      Resource<User> user;

      if (request.UserId != null)
      {
        user = await Internal_EnsureFirst(
          transaction,
          Resources.Query<User>(
            transaction,
            (query) => query.Where((user) => user.Id == request.UserId)
          )
        );
      }
      else if (request.Username != null)
      {
        user = await Internal_EnsureFirst(
          transaction,
          Resources.Query<User>(
            transaction,
            (query) =>
              query.Where(
                (user) =>
                  user.Username.Equals(
                    request.Username,
                    System.StringComparison.CurrentCultureIgnoreCase
                  )
              )
          )
        );
      }
      else
      {
        throw new ConnectionResponseException(
          ResponseCode.InvalidParameters,
          new ConnectionResponseExceptionData.InvalidParameters()
        );
      }

      if (
        await Resources
          .Query<PasswordResetRequest>(
            transaction,
            (query) => query.Where((request) => request.UserId == user.Id)
          )
          .AnyAsync()
      )
      {
        throw new ConnectionResponseException(
          ResponseCode.InvalidParameters,
          new ConnectionResponseExceptionData.InvalidParameters()
        );
      }

      await Resources.CreatePasswordResetRequest(transaction, user);

      return new() { };
    };
}
