using System;
using System.Collections.Generic;

namespace RizzziGit.EnderDrive.Utilities;

public static class EnumerableExtensions
{
  public delegate IEnumerable<T> OptionalCallback<T>(IEnumerable<T> enumerable);
  public delegate IAsyncEnumerable<T> AsyncOptionalCallback<T>(IAsyncEnumerable<T> enumerable);

  public static IEnumerable<T> If<T>(
    this IEnumerable<T> enumerable,
    Func<bool> func,
    OptionalCallback<T> callback
  ) => Optional(enumerable, func() ? callback : null);

  public static IEnumerable<T> Optional<T>(
    this IEnumerable<T> enumerable,
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
