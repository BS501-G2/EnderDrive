using System;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;

namespace RizzziGit.EnderDrive.Server.Connections;

using MongoDB.Bson;
using Resources;

public sealed partial class Connection
{
    private sealed record class GetFileRequest
    {
        [BsonElement("fileId")]
        public required ObjectId? FileId;
    }

    private sealed record class GetFileResponse
    {
        [BsonElement("file")]
        public required string File;
    }

    private AuthenticatedRequestHandler<GetFileRequest, GetFileResponse> GetFile =>
        async (transaction, request, userAuthentication, me) =>
        {
            File file = await Internal_GetFile(transaction, me, request.FileId);

            FileAccessResult result =
                await Resources.FindFileAccess(transaction, file, me, userAuthentication)
                ?? throw new InvalidOperationException("No access to this file");

            return new() { File = JToken.FromObject(file).ToString() };
        };
}
