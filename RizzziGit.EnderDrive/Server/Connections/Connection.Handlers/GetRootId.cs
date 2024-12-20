using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using RizzziGit.EnderDrive.Server.Resources;

namespace RizzziGit.EnderDrive.Server.Connections;

public sealed partial class Connection
{
  private sealed record class GetRootIdRequest
  {
    public required ObjectId UserId;
  }

  private sealed record class GetRootIdResponse
  {
    public required ObjectId? FileId;
  }

  private AuthenticatedRequestHandler<GetRootIdRequest, GetRootIdResponse> GetRootId =>
    async (transaction, request, userAuthentication, me, myAdminAccess) =>
    {
      if (request.UserId != me.Id && myAdminAccess == null)
      {
        throw new AdminRequired();
      }

      Resource<User> user = await Internal_EnsureFirst(
        transaction,
        Resources.Query<User>(
          transaction,
          (query) => query.Where((user) => user.Id == request.UserId)
        )
      );

      return new() { FileId = user.Data.RootFileId };
    };
}
