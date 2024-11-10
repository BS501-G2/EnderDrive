using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

using Resources;

public sealed partial class Connection
{
    private sealed class GetFileMimeRequest
    {
        [BsonElement("fileId")]
        public required ObjectId FileId;
    }

    private sealed class GetFileMimeResponse
    {
        [BsonElement("fileMimeType")]
        public required string FileMimeType;
    }

    private TransactedRequestHandler<GetFileMimeRequest, GetFileMimeResponse> GetFileMime =>
        async (transaction, request) =>
        {
            ConnectionContext context = GetContext();

            UnlockedUserAuthentication userAuthentication = Internal_EnsureAuthentication();
            User me = await Internal_Me(transaction, userAuthentication);

            File file = await Internal_GetFile(transaction, me, request.FileId);
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

            System.IO.Stream stream = await Resources.CreateReadStream(
                transaction,
                fileAccessResult.File,
                fileContent,
                fileSnapshot
            );

            MimeDetective.Storage.Definition? definition = await Server.MimeDetector.Inspect(
                stream,
                transaction.CancellationToken
            );

            return new() { FileMimeType = definition?.File.MimeType ?? "application/octet-stream" };
        };
}
