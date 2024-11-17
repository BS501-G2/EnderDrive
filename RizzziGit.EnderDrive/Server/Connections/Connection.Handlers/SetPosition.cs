using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

public sealed partial class Connection
{
	private sealed record class SetPositionRequest
	{
		[BsonElement(
			"streamId"
		)]
		public required ObjectId StreamId;

		[BsonElement(
			"newPosition"
		)]
		public required long NewPosition;
	}

	private sealed record class SetPositionResponse { }

	private RequestHandler<
		SetPositionRequest,
		SetPositionResponse
	> SetPosition =>
		async (
			request,
			cancellationToken
		) =>
		{
			ConnectionContext context =
				GetContext();

			if (
				!context.FileStreams.TryGetValue(
					request.StreamId,
					out ConnectionByteStream? stream
				)
			)
			{
				throw new InvalidOperationException(
					"File stream not found."
				);
			}

			await stream.SetPosition(
				request.NewPosition,
				cancellationToken
			);

			return new();
		};
}
