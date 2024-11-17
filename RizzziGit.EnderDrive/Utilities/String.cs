using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using RizzziGit.EnderDrive.Server.Resources;

namespace RizzziGit.EnderDrive.Utilities;

public static class StringExtensions
{
	public static string[] ToJson<T>(
		this T[] items
	)
		where T : ResourceData =>
		items
			.Select(
				(
					item
				) =>
					item.ToJson()
			)
			.ToArray();

	public static string ToJson<T>(
		this T item
	)
		where T : ResourceData =>
		JToken
			.FromObject(
				item
			)
			.ToString();
}
