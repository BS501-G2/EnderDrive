using System;
using System.Collections.Generic;
using System.Linq;

namespace RizzziGit.EnderDrive.Utilities;

public static class EnumerableExtensions
{
  public delegate IQueryable<T> OptionalCallback<T>(IQueryable<T> enumerable);
  public delegate IAsyncEnumerable<T> AsyncOptionalCallback<T>(IAsyncEnumerable<T> enumerable);

  public static IQueryable<T> If<T>(
    this IQueryable<T> enumerable,
    Func<bool> func,
    OptionalCallback<T> callback
  ) => Optional(enumerable, func() ? callback : null);

  public static IQueryable<T> Optional<T>(
    this IQueryable<T> enumerable,
    OptionalCallback<T>? callback
  ) => callback == null ? enumerable : callback(enumerable);

  public static IAsyncEnumerable<T> IfAwait<T>(
    this IAsyncEnumerable<T> enumerable,
    Func<bool> func,
    AsyncOptionalCallback<T> callback
  ) => OptionalAwait(enumerable, func() ? callback : null);

  public static IAsyncEnumerable<T> OptionalAwait<T>(
    this IAsyncEnumerable<T> enumerable,
    AsyncOptionalCallback<T>? callback
  ) => callback == null ? enumerable : callback(enumerable);
}
