using System;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

public sealed partial class Connection
{
  private sealed record class UpdateNameRequest
  {
    [BsonElement("firstName")]
    public required string FirstName;

    [BsonElement("middleName")]
    public required string? MiddleName;

    [BsonElement("lastName")]
    public required string LastName;

    [BsonElement("displayName")]
    public required string? DisplayName;
  }

  private sealed record class UpdateNameResponse { }

  private AuthenticatedRequestHandler<UpdateNameRequest, UpdateNameResponse> UpdateName =>
    async (transaction, request, userAuthentication, me, _) =>
    {
      if (request.FirstName.Length == 0)
      {
        throw new InvalidOperationException("Invalid FirstName");
      }

      me.Data.FirstName = request.FirstName;

      if (!(request.MiddleName != null && request.MiddleName.Length != 0))
      {
        me.Data.MiddleName = null;
      }

      me.Data.MiddleName = request.MiddleName;

      if (request.LastName.Length == 0)
      {
        throw new InvalidOperationException("Invalid Last Name");
      }

      me.Data.LastName = request.LastName;

      if (!(request.DisplayName != null && request.DisplayName.Length != 0))
      {
        me.Data.MiddleName = null;
      }

      me.Data.DisplayName = request.DisplayName;

      await me.Save(transaction);

      return new() { };
    };
}
