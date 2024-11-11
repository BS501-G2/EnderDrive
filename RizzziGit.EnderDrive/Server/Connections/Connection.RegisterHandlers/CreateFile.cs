using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

using Commons.Collections;
using Commons.Memory;
using Commons.Utilities;
using Resources;

public sealed partial class Connection
{
    private sealed record class CreateFileRequest
    {
        [BsonElement("parentFolderId")]
        public required ObjectId ParentFolderId;

        [BsonElement("name")]
        public required string Name;
    }

    private sealed record class CreateFileResponse
    {
        [BsonElement("streamId")]
        public required ObjectId StreamId;
    }

    private AuthenticatedRequestHandler<CreateFileRequest, CreateFileResponse> CreateFile =>
        async (transaction, request, userAuthentication, me) =>
        {
            File parentFolder = await Internal_GetFile(transaction, me, request.ParentFolderId);

            FileAccessResult access = await Internal_UnlockFile(
                transaction,
                parentFolder,
                me,
                userAuthentication
            );

            UnlockedFile unlockedFile = await Resources.CreateFile(
                transaction,
                me,
                access.File,
                FileType.File,
                request.Name
            );

            FileContent fileContent = await Resources.GetMainFileContent(transaction, unlockedFile);

            FileSnapshot fileSnapshot = await Resources.CreateFileSnapshot(
                transaction,
                unlockedFile,
                fileContent,
                userAuthentication,
                null
            );

            TaskCompletionSource<ObjectId> source = new();

            RunStream(unlockedFile, fileContent, fileSnapshot, userAuthentication, source);

            return new() { StreamId = await source.Task };
        };

}
