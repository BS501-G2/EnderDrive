using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

using System.Linq;
using MongoDB.Bson;
using Resources;

public sealed partial class Connection
{
    private sealed record class GetLatestFileSnapshotRequest
    {
        [BsonElement("fileId")]
        public required ObjectId FileId;

        [BsonElement("fileContentId")]
        public required ObjectId? FileContentId;
    }

    private sealed record class GetLatestFileSnapshotResponse
    {
        [BsonElement("fileSnapshot")]
        public required string? FileSnapshot;
    }

    private AuthenticatedRequestHandler<
        GetLatestFileSnapshotRequest,
        GetLatestFileSnapshotResponse
    > GetLatestFileSnapshot =>
        async (transaction, request, userAuthentication, me) =>
        {
            File file = await Internal_GetFile(transaction, me, request.FileId);
            FileAccessResult fileAccessResult = await Internal_UnlockFile(
                transaction,
                file,
                me,
                userAuthentication
            );

            FileContent fileContent =
                request.FileContentId != null
                    ? await Internal_EnsureFirst(
                        transaction,
                        Resources.GetFileContents(transaction, file, id: request.FileContentId)
                    )
                    : await Resources.GetMainFileContent(transaction, file);

            FileSnapshot? fileSnapshot = await Resources.GetLatestFileSnapshot(
                transaction,
                file,
                fileContent
            );

            return new() { FileSnapshot = fileSnapshot?.ToJson() };
        };
}
