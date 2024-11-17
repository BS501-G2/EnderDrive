using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

using Resources;

public sealed partial class Connection
{
	private sealed record class DeleteNewsRequest
	{
		[BsonElement(
			"newsId"
		)]
		public required ObjectId NewsId;
	}

	private sealed record class DeleteNewsResponse { }

	private AuthenticatedRequestHandler<
		DeleteNewsRequest,
		DeleteNewsResponse
	> DeleteNews =>
		async (
			transaction,
			request,
			userAuthentication,
			me,
			myAdminAccess
		) =>
		{
			News news =
				await Internal_EnsureFirst(
					transaction,
					Resources.GetNews(
						transaction
					)
				);

			await Resources.DeleteNews(
				transaction,
				news
			);

			return new()
			{ };
		};
}
