using System.Linq;
using MongoDB.Bson.Serialization.Attributes;
using RizzziGit.EnderDrive.Server.Resources;
using RizzziGit.EnderDrive.Utilities;

namespace RizzziGit.EnderDrive.Server.Connections;

public sealed partial class Connection
{
  private sealed record class GetPasswordResetRequestsRequest
  {
    [BsonElement("status")]
    public required PasswordResetRequestStatus? Status;

    [BsonElement("pagination")]
    public required PaginationOptions? Pagination;
  }

  private sealed record class GetPasswordResetRequestsResponse
  {
    [BsonElement("requests")]
    public required string[] Requests;
  }

  private AdminRequestHandler<
    GetPasswordResetRequestsRequest,
    GetPasswordResetRequestsResponse
  > GetPasswordResetRequests =>
    async (transaction, request, userAuthentication, me, myAdminAccess) =>
    {
      Resource<PasswordResetRequest>[] requests = await Resources
        .Query<PasswordResetRequest>(
          transaction,
          (query) =>
            query
              .Where((item) => request.Status == null || item.Status == request.Status)
              .ApplyPagination(request.Pagination)
        )
        .ToArrayAsync(transaction);

      return new() { Requests = [.. requests.ToJson()] };
    };
}
