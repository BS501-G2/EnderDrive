using System;
using ClamAV.Net.Client.Results;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;
using RizzziGit.EnderDrive.Server.Resources;

namespace RizzziGit.EnderDrive.Server.Connections;

public sealed partial class Connection
{
    private sealed record class ScanFileRequest
    {
        [BsonElement("fileId")]
        public required ObjectId FileId;

        [BsonElement("fileContentId")]
        public required ObjectId FileContentId;

        [BsonElement("fileSnapshotId")]
        public required ObjectId FileSnapshotId;
    };

    private sealed record class ScanFileResponse
    {
        [BsonElement("result")]
        public required string Result;
    };

    private TransactedRequestHandler<ScanFileRequest, ScanFileResponse> ScanFile =>
        async (transaction, request) =>
        {
            UnlockedUserAuthentication userAuthentication = Internal_EnsureAuthentication();
            User me = await Internal_Me(transaction, userAuthentication);
            File file = await Internal_GetFile(transaction, me, request.FileId);

            FileAccessResult result = await Internal_UnlockFile(
                transaction,
                file,
                me,
                userAuthentication
            );

            FileContent fileContent = await Internal_GetFirst(
                transaction,
                Resources.GetFileContents(transaction, file, id: request.FileContentId)
            );

            FileSnapshot fileSnapshot = await Internal_GetFirst(
                transaction,
                Resources.GetFileSnapshots(transaction, file, fileContent, request.FileSnapshotId)
            );

            return new()
            {
                Result = JToken
                    .FromObject(
                        await Server.VirusScanner.Scan(
                            transaction,
                            result.File,
                            fileContent,
                            fileSnapshot,
                            false,
                            transaction
                        )
                    )
                    .ToString(),
            };
        };
}
