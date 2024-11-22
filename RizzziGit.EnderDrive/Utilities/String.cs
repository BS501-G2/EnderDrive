using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using RizzziGit.EnderDrive.Server.Resources;

namespace RizzziGit.EnderDrive.Utilities;

public static class StringExtensions
{
  public static string[] ToJson<T>(this T[] items)
    where T : ResourceData => [.. items.Select((item) => item.ToJson())];

  public static string ToJson<T>(this T item)
    where T : ResourceData => JToken.FromObject(item).ToString();

  public static string ToReadableBytes(this long bytes)
  {
    string[] unit = ["B", "KiB", "MiB", "GiB", "TiB"];
    decimal size = bytes;
    int count = 0;

    while (size >= 1000)
    {
      size /= 1024;
      count++;
    }
    return $"{size:0.##} {unit[count]}";
  }
}
