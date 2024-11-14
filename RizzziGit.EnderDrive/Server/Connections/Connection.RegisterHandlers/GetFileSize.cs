using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

using Resources;

public sealed partial class Connection
{
    private sealed record class GetFileSizeRequest
    {
        [BsonElement("fileId")]
        public required ObjectId FileId;

        [BsonElement("fileContentId")]
        public required ObjectId FileContentId;

        [BsonElement("fileSnapshotId")]
        public required ObjectId FileSnapshotId;
    }

    private sealed record class GetFileSizeResponse
    {
        [BsonElement("size")]
        public required long Size;
    }

    private AuthenticatedRequestHandler<GetFileSizeRequest, GetFileSizeResponse> GetFileSize =>
        async (transaction, request, userAuthentication, me) =>
        {
            File file = await Internal_GetFile(transaction, me, request.FileId);
            FileContent fileContent = await Resources.GetMainFileContent(transaction, file);
            FileSnapshot? fileSnapshot = await Resources
                .GetFileSnapshots(transaction, file, fileContent, request.FileSnapshotId)
                .ToAsyncEnumerable()
                .FirstOrDefaultAsync(transaction.CancellationToken);

            return new()
            {
                Size =
                    fileSnapshot != null
                        ? await Resources.GetFileSize(transaction, fileSnapshot)
                        : 0,
            };
        };
}
