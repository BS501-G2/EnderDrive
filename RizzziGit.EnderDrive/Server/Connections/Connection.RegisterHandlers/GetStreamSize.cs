using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

public sealed partial class Connection
{
    private sealed record class GetStreamSizeRequest
    {
        [BsonElement("streamId")]
        public required ObjectId StreamId;
    }

    private sealed record class GetStreamSizeResponse
    {
        [BsonElement("size")]
        public required long Size;
    }

    private RequestHandler<GetStreamSizeRequest, GetStreamSizeResponse> GetStreamSize =>
        async (request, cancellationToken) =>
        {
            ConnectionContext context = GetContext();

            if (
                !context.FileStreams.TryGetValue(request.StreamId, out ConnectionByteStream? stream)
            )
            {
                throw new InvalidOperationException("File stream not found.");
            }

            return new() { Size = await stream.GetLength(cancellationToken) };
        };
}
