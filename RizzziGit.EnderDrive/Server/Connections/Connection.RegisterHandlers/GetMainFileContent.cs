using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

using Server.Resources;
using Utilities;

public sealed partial class Connection
{
    private sealed record class GetMainFileContentRequest
    {
        [BsonElement("fileId")]
        public required ObjectId FileId;
    }

    private sealed record class GetMainFileContentResonse
    {
        [BsonElement("fileContent")]
        public required string FileContent;
    }

    private AuthenticatedRequestHandler<
        GetMainFileContentRequest,
        GetMainFileContentResonse
    > GetMainFileContent =>
        async (transaction, request, userAuthentication, me) =>
        {
            File file = await Internal_GetFile(transaction, me, request.FileId);
            FileAccessResult fileAccessResult = await Internal_UnlockFile(
                transaction,
                file,
                me,
                userAuthentication
            );

            FileContent fileContent = await Resources.GetMainFileContent(
                transaction,
                fileAccessResult.File
            );
            return new() { FileContent = fileContent.ToJson() };
        };
}
