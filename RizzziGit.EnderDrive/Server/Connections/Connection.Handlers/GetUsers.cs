using System.Collections;
using System.Collections.Generic;
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
    public required string? SearchString;
    public required UserRole[]? IncludeRole;
    public required UserRole[]? ExcludeRole;
    public required string? Username;
    public required ObjectId? Id;
    public required PaginationOptions? Pagination;
    public bool ExcludeSelf = true;
  }

  private sealed record class GetUsersResponse
  {
    public required string[] Users;
  }

  private AuthenticatedRequestHandler<GetUsersRequest, GetUsersResponse> GetUsers =>
    async (transaction, request, _, me, _) =>
    {
      IEnumerable<string> keywords =
        request.SearchString?.Split(' ').Where((str) => str.Length > 0) ?? [];

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
                    || keywords
                      .Where(
                        (str) =>
                          user.Username.Contains(
                            str,
                            System.StringComparison.CurrentCultureIgnoreCase
                          )
                      )
                      .Any()
                    || keywords
                      .Where(
                        (str) =>
                          user.FirstName.Contains(
                            str,
                            System.StringComparison.CurrentCultureIgnoreCase
                          )
                      )
                      .Any()
                    || (
                      user.MiddleName != null
                      && keywords
                        .Where(
                          (str) =>
                            user.MiddleName.Contains(
                              str,
                              System.StringComparison.CurrentCultureIgnoreCase
                            )
                        )
                        .Any()
                    )
                    || keywords
                      .Where(
                        (str) =>
                          user.LastName.Contains(
                            str,
                            System.StringComparison.CurrentCultureIgnoreCase
                          )
                      )
                      .Any()
                    || (
                      user.DisplayName != null
                      && keywords
                        .Where(
                          (str) =>
                            user.DisplayName.Contains(
                              str,
                              System.StringComparison.CurrentCultureIgnoreCase
                            )
                        )
                        .Any()
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
                    || keywords
                      .Where(
                        (str) =>
                          user.Username.Equals(str, System.StringComparison.OrdinalIgnoreCase)
                      )
                      .Any()
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
