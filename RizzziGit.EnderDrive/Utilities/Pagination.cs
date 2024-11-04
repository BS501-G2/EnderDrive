using System.Linq;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Utilities;

public sealed record class PaginationOptions
{
    public static int GetCount(PaginationOptions? pagination) => pagination?.Count ?? DEFAULT_COUNT;

    public static int GetOffset(PaginationOptions? pagination) =>
        pagination?.Offset ?? DEFAULT_OFFSET;

    public const int DEFAULT_COUNT = 25;
    public const int DEFAULT_OFFSET = 0;

    private int count = DEFAULT_COUNT;
    private int offset = DEFAULT_OFFSET;

    [BsonElement("count")]
    public required int? Count
    {
        get => count;
        set => count = int.Clamp(value ?? 0, 10, 100);
    }

    [BsonElement("offset")]
    public required int? Offset
    {
        get => offset;
        set => offset = int.Max(value ?? 0, 0);
    }
}

public static class IQueryableExtensions
{
    public static IQueryable<T> ApplyPagination<T>(
        this IQueryable<T> queryable,
        PaginationOptions? pagination
    ) =>
        queryable
            .Skip(PaginationOptions.GetOffset(pagination))
            .Take(PaginationOptions.GetCount(pagination));

    public static IQueryable<T> ApplyToQueryable<T>(
        this PaginationOptions pagination,
        IQueryable<T> queryable
    ) =>
        queryable
            .Skip(PaginationOptions.GetOffset(pagination))
            .Take(PaginationOptions.GetCount(pagination));
}
