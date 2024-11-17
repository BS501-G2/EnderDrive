using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;
using RizzziGit.EnderDrive.Server.Resources;

namespace RizzziGit.EnderDrive.Server.Connections;

public sealed partial class Connection
{
	private sealed record class CreateFolderRequest
		: BaseFileRequest
	{
		[BsonElement(
			"name"
		)]
		public required string Name;
	}

	private sealed record class CreateFolderResponse
	{
		[BsonElement(
			"file"
		)]
		public required string File;
	}

	private FileRequestHandler<
		CreateFolderRequest,
		CreateFolderResponse
	> CreateFolder =>
		async (
			transaction,
			request,
			userAuthentication,
			me,
			myAdminAccess,
			file,
			result
		) =>
		{
			ConnectionContext context =
				GetContext();

			if (
				file.Type
				!= FileType.Folder
			)
			{
				throw new InvalidOperationException(
					"Parent is not a folder."
				);
			}

			UnlockedFile newFile =
				await Resources.CreateFile(
					transaction,
					me,
					result.File,
					FileType.Folder,
					request.Name
				);

			return new()
			{
				File =
					JToken
						.FromObject(
							newFile.Original
						)
						.ToString(),
			};
		};
}
