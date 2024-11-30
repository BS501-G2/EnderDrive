using System.Linq;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Utilities;

public sealed record class PaginationOptions
{
  public const int DEFAULT_COUNT = 25;
  public const int DEFAULT_OFFSET = 0;

  // private int? count = DEFAULT_COUNT;
  // private int? offset = DEFAULT_OFFSET;

  public required int? Count;
  public required int? Offset;

  // [BsonElement("count")]
  // public required int? Count
  // {
  //     get => count;
  //     set => count = value != null ? int.Clamp(value ?? 0, 10, 100) : null;
  // }

  // [BsonElement("offset")]
  // public required int? Offset
  // {
  //     get => offset;
  //     set => offset = offset != null ? int.Max(value ?? 0, 0) : null;
  // }
}

public static class IQueryableExtensions
{
  public static IQueryable<T> ApplyPagination<T>(
    this IQueryable<T> queryable,
    PaginationOptions? pagination
  )
  {
    if (pagination != null)
    {
      if (pagination.Offset != null)
      {
        queryable = queryable.Skip(pagination.Offset.Value);
      }

      if (pagination.Count != null)
      {
        queryable = queryable.Take(pagination.Count.Value);
      }
    }

    return queryable;
  }
}
