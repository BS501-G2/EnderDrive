using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;
using RizzziGit.EnderDrive.Server.Resources;

namespace RizzziGit.EnderDrive.Server.Connections;

public sealed partial class Connection
{
    private sealed class CreateFolderRequest
    {
        [BsonElement("parentFileId")]
        public required ObjectId ParentFileId;

        [BsonElement("name")]
        public required string Name;
    }

    private sealed class CreateFolderResponse
    {
        [BsonElement("file")]
        public required string File;
    }

    private TransactedRequestHandler<CreateFolderRequest, CreateFolderResponse> CreateFolder =>
        async (transaction, request) =>
        {
            ConnectionContext context = GetContext();

            UnlockedUserAuthentication userAuthentication = Internal_EnsureAuthentication();
            User me = await Internal_Me(transaction, userAuthentication);

            File parentFolder = await Internal_GetFile(transaction, me, request.ParentFileId);
            FileAccessResult fileAccessResult = await Internal_UnlockFile(
                transaction,
                parentFolder,
                me,
                userAuthentication
            );

            if (parentFolder.Type != FileType.Folder)
            {
                throw new InvalidOperationException("Parent is not a folder.");
            }

            UnlockedFile file = await Resources.CreateFile(
                transaction,
                me,
                fileAccessResult.File,
                FileType.Folder,
                request.Name
            );

            return new() { File = JToken.FromObject(file.Original).ToString() };
        };
}
