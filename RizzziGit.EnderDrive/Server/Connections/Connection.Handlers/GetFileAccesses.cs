using System;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;

namespace RizzziGit.EnderDrive.Server.Connections;

using Resources;
using Utilities;

public sealed partial class Connection
{
	private sealed record class GetFileAccessesRequest
	{
		[BsonElement(
			"targetUserId"
		)]
		public required ObjectId? TargetUserId;

		[BsonElement(
			"targetFileId"
		)]
		public required ObjectId? TargetFileId;

		[BsonElement(
			"authorUserId"
		)]
		public required ObjectId? AuthorUserId;

		[BsonElement(
			"level"
		)]
		public required FileAccessLevel? Level;

		[BsonElement(
			"pagination"
		)]
		public required PaginationOptions? Pagination;
	};

	private sealed record class GetFileAccessesResponse
	{
		[BsonElement(
			"fileAccesses"
		)]
		public required string[] FileAccesses;
	};

	private AuthenticatedRequestHandler<
		GetFileAccessesRequest,
		GetFileAccessesResponse
	> GetFileAccesses =>
		async (
			transaction,
			request,
			userAuthentication,
			me,
			myAdminAccess
		) =>
		{
			User? targetUser;
			if (
				request.TargetUserId
					!= me.Id
				&& myAdminAccess
					== null
			)
			{
				throw new InvalidOperationException(
					"Target user must be set to self when not an admin."
				);
			}

			targetUser =
				request.TargetUserId
				!= null
					? await Internal_EnsureFirst(
						transaction,
						Resources.GetUsers(
							transaction,
							id: request.TargetFileId
						)
					)
					: null;

			File? targetFile =
				request.TargetFileId
				!= null
					? await Internal_GetFile(
						transaction,
						me,
						request.TargetFileId
					)
					: null;

			User? authorUser =
				null;

			if (
				request.TargetUserId
				!= null
			)
			{
				targetUser =
					Internal_EnsureExists(
						await Resources
							.GetUsers(
								transaction,
								id: request.TargetUserId
							)
							.ToAsyncEnumerable()
							.FirstOrDefaultAsync(
								transaction.CancellationToken
							)
					);
			}

			if (
				request.AuthorUserId
				!= null
			)
			{
				if (
					targetUser
						== null
					&& !await Resources
						.GetAdminAccesses(
							transaction,
							userId: me.Id
						)
						.ToAsyncEnumerable()
						.AnyAsync(
							transaction.CancellationToken
						)
				)
				{
					throw new InvalidOperationException(
						"Target user must be set if not an admin."
					);
				}

				authorUser =
					Internal_EnsureExists(
						await Resources
							.GetUsers(
								transaction,
								id: request.AuthorUserId
							)
							.ToAsyncEnumerable()
							.FirstOrDefaultAsync(
								transaction.CancellationToken
							)
					);
			}

			FileAccess[] fileAccesses =
				await Resources
					.GetFileAccesses(
						transaction,
						targetUser,
						targetFile: targetFile,
						authorUser: authorUser,
						level: request.Level
					)
					.ApplyPagination(
						request.Pagination
					)
					.ToAsyncEnumerable()
					.WhereAwait(
						async (
							fileAccess
						) =>
						{
							File file =
								await Internal_GetFile(
									transaction,
									me,
									fileAccess.FileId
								);

							FileAccessResult? fileAccessResult =
								await Resources.FindFileAccess(
									transaction,
									file,
									me,
									userAuthentication,
									FileAccessLevel.Read
								);

							return fileAccessResult
								!= null;
						}
					)
					.ToArrayAsync(
						transaction.CancellationToken
					);

			return new()
			{
				FileAccesses =
					fileAccesses
						.Select(
							(
								fileAccess
							) =>
								JToken
									.FromObject(
										fileAccess
									)
									.ToString()
						)
						.ToArray(),
			};
		};
}
