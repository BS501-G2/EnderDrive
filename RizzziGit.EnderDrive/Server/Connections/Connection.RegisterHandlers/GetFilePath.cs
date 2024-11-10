using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;

namespace RizzziGit.EnderDrive.Server.Connections;

using Resources;
using Utilities;

public sealed partial class Connection
{
    private sealed record class GetFilePathRequest
    {
        [BsonElement("fileId")]
        public required ObjectId FileId;

        [BsonElement("pagination")]
        public required PaginationOptions? Pagination;
    }

    private sealed record class GetFilePathResponse
    {
        [BsonElement("path")]
        public required string[] Path;
    }

    private TransactedRequestHandler<GetFilePathRequest, GetFilePathResponse> GetFilePath =>
        async (transaction, request) =>
        {
            UnlockedUserAuthentication userAuthentication = Internal_EnsureAuthentication();
            User me = await Internal_Me(transaction, userAuthentication);

            File currentFile = await Internal_GetFile(transaction, me, request.FileId);
            FileAccessResult result =
                await Resources.FindFileAccess(
                    transaction,
                    currentFile,
                    me,
                    userAuthentication,
                    FileAccessLevel.Read
                )
                ?? throw new InvalidOperationException(
                    "Insufficient permissions to access the file."
                );

            File rootFile =
                result.FileAccess != null
                    ? result.File
                    : await Resources.GetRootFolder(transaction, me);

            List<File> path = [currentFile];
            while (true)
            {
                currentFile = await Internal_GetFile(transaction, me, currentFile.ParentId);

                if (path.Last().Id != currentFile.Id)
                {
                    path.Add(currentFile);
                }

                if (currentFile.Id == rootFile.Id)
                {
                    break;
                }
            }

            return new()
            {
                Path = path.Reverse<File>()
                    .Select((entry) => JToken.FromObject(entry).ToString())
                    .ToArray(),
            };
        };
}
