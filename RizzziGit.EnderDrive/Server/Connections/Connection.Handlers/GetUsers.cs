using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;

namespace RizzziGit.EnderDrive.Server.Connections;

using Resources;
using Utilities;

public sealed partial class Connection
{
  private sealed record class GetUsersRequest
  {
    [BsonElement("searchString")]
    public required string? SearchString;

    [BsonElement("includeRole")]
    public required UserRole[]? IncludeRole;

    [BsonElement("excludeRole")]
    public required UserRole[]? ExcludeRole;

    [BsonElement("username")]
    public required string? Username;

    [BsonElement("id")]
    public required ObjectId? Id;

    [BsonElement("pagination")]
    public required PaginationOptions? Pagination;

    [BsonElement("excludeSelf")]
    public bool ExcludeSelf = true;
  }

  private sealed record class GetUsersResponse
  {
    [BsonElement("users")]
    public required string[] Users;
  }

  private AuthenticatedRequestHandler<GetUsersRequest, GetUsersResponse> GetUsers =>
    async (transaction, request, _, me, _) =>
    {
      Resource<User>[] users = await Resources
        .Query<User>(
          transaction,
          (query) =>
            query
              .Where(
                (user) =>
                  (
                    request.SearchString == null
                    || request.SearchString.Length == 0
                    || user.Username.Contains(
                      request.SearchString,
                      System.StringComparison.CurrentCultureIgnoreCase
                    )
                    || user.FirstName.Contains(
                      request.SearchString,
                      System.StringComparison.CurrentCultureIgnoreCase
                    )
                    || (
                      user.MiddleName != null
                      && user.MiddleName.Contains(
                        request.SearchString,
                        System.StringComparison.CurrentCultureIgnoreCase
                      )
                    )
                    || user.LastName.Contains(
                      request.SearchString,
                      System.StringComparison.CurrentCultureIgnoreCase
                    )
                    || (
                      user.DisplayName != null
                      && user.DisplayName.Contains(
                        request.SearchString,
                        System.StringComparison.CurrentCultureIgnoreCase
                      )
                    )
                  )
                  && (
                    request.IncludeRole == null
                    || request.IncludeRole.Length == 0
                    || request.IncludeRole.All((item) => user.Roles.Contains(item))
                  )
                  && (
                    request.ExcludeRole == null
                    || request.ExcludeRole.Length == 0
                    || !request.ExcludeRole.Intersect(user.Roles).Any()
                  )
                  && (
                    request.Username == null
                    || user.Username.Equals(
                      request.Username,
                      System.StringComparison.OrdinalIgnoreCase
                    )
                  )
                  && (request.Id == null || user.Id == request.Id)
                  && (!request.ExcludeSelf || user.Id != me.Id)
              )
              .ApplyPagination(request.Pagination)
        )
        .ToArrayAsync(transaction.CancellationToken);

      return new() { Users = [.. users.ToJson()] };
    };
}
