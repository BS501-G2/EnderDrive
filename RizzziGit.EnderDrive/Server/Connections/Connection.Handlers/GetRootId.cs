using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using RizzziGit.EnderDrive.Server.Resources;

namespace RizzziGit.EnderDrive.Server.Connections;

public sealed partial class Connection
{
  private sealed record class GetRootIdRequest
  {
    [BsonElement("userId")]
    public required ObjectId UserId;
  }

  private sealed record class GetRootIdResponse
  {
    [BsonElement("fileId")]
    public required ObjectId? FileId;
  }

  private AdminRequestHandler<GetRootIdRequest, GetRootIdResponse> GetRootId =>
    async (transaction, request, userAuthentication, me, _) =>
    {
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
