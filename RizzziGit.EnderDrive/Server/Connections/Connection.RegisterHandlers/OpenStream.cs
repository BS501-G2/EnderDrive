using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

using System.Threading.Tasks;
using Resources;

public sealed partial class Connection
{
    private sealed record class OpenStreamRequest
    {
        [BsonElement("fileId")]
        public required ObjectId FileId;

        [BsonElement("fileContentId")]
        public required ObjectId FileContentId;

        [BsonElement("fileSnapshotId")]
        public required ObjectId FileSnapshotId;
    }

    private sealed record class OpenStreamResponse
    {
        [BsonElement("streamId")]
        public required ObjectId StreamId;
    }

    private AuthenticatedRequestHandler<OpenStreamRequest, OpenStreamResponse> OpenStream =>
        async (transaction, request, userAuthentication, me) =>
        {
            ConnectionContext context = GetContext();

            File file = await Internal_EnsureFirst(
                transaction,
                Resources.GetFiles(transaction, id: request.FileId)
            );

            FileAccessResult fileAccessResult = await Internal_UnlockFile(
                transaction,
                file,
                me,
                userAuthentication
            );

            FileContent fileContent = await Resources.GetMainFileContent(transaction, file);
            FileSnapshot fileSnapshot =
                await Resources.GetLatestFileSnapshot(transaction, file, fileContent)
                ?? await Resources.CreateFileSnapshot(
                    transaction,
                    fileAccessResult.File,
                    fileContent,
                    userAuthentication,
                    null
                );

            TaskCompletionSource<ObjectId> source = new();

            RunStream(fileAccessResult.File, fileContent, fileSnapshot, userAuthentication, source);

            return new() { StreamId = await source.Task };
        };
}
