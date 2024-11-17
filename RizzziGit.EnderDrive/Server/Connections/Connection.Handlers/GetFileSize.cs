using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

using Resources;

public sealed partial class Connection
{
	private sealed record class GetFileSizeRequest
		: BaseFileRequest
	{
		[BsonElement(
			"fileContentId"
		)]
		public required ObjectId? FileContentId;

		[BsonElement(
			"fileSnapshotId"
		)]
		public required ObjectId? FileSnapshotId;
	}

	private sealed record class GetFileSizeResponse
	{
		[BsonElement(
			"size"
		)]
		public required long Size;
	}

	private FileRequestHandler<
		GetFileSizeRequest,
		GetFileSizeResponse
	> GetFileSize =>
		async (
			transaction,
			request,
			userAuthentication,
			me,
			_,
			file,
			fileAccessResult
		) =>
		{
			FileContent fileContent =
				request.FileContentId
				!= null
					? await Internal_EnsureFirst(
						transaction,
						Resources.GetFileContents(
							transaction,
							file,
							id: request.FileContentId
						)
					)
					: await Resources.GetMainFileContent(
						transaction,
						file
					);

			FileSnapshot? fileSnapshot =
				request.FileSnapshotId
				!= null
					? await Resources
						.GetFileSnapshots(
							transaction,
							file,
							fileContent,
							request.FileSnapshotId
						)
						.ToAsyncEnumerable()
						.FirstOrDefaultAsync(
							transaction.CancellationToken
						)
					: await Resources.GetLatestFileSnapshot(
						transaction,
						file,
						fileContent
					);

			return new()
			{
				Size =
					fileSnapshot
					!= null
						? await Resources.GetFileSize(
							transaction,
							fileSnapshot
						)
						: 0,
			};
		};
}
