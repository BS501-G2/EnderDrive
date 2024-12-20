using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

using Resources;

public sealed partial class Connection
{
  private sealed record class DeleteNewsRequest
  {
    public required ObjectId NewsId;
  }

  private sealed record class DeleteNewsResponse { }

  private AuthenticatedRequestHandler<DeleteNewsRequest, DeleteNewsResponse> DeleteNews =>
    async (transaction, request, userAuthentication, me, myAdminAccess) =>
    {
      Resource<News> news = await Internal_EnsureFirst(
        transaction,
        Resources.Query<News>(
          transaction,
          (query) => query.Where((item) => item.Id == request.NewsId)
        )
      );

      await Resources.Delete(transaction, news);

      return new() { };
    };
}
