using System;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;

namespace RizzziGit.EnderDrive.Server.Connections;

using Resources;

public sealed partial class Connection
{
    private sealed record class GetFileRequest
    {
        [BsonElement("fileId")]
        public required string? FileId;
    }

    private sealed record class GetFileResponse
    {
        [BsonElement("file")]
        public required string File;
    }

    private TransactedRequestHandler<GetFileRequest, GetFileResponse> GetFile =>
        async (transaction, request) =>
        {
            UnlockedUserAuthentication userAuthentication = Internal_EnsureAuthentication();
            User me = await Internal_Me(transaction, userAuthentication);
            File file = await Internal_GetFile(transaction, me, Internal_ParseId(request.FileId));

            FileAccessResult result =
                await Resources.FindFileAccess(transaction, file, me, userAuthentication)
                ?? throw new InvalidOperationException("No access to this file");

            return new() { File = JToken.FromObject(file).ToString() };
        };
}
