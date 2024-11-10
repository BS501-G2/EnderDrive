using System;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

using Resources;
using RizzziGit.EnderDrive.Utilities;

public sealed partial class Connection
{
    private sealed record class GetFileLogsRequest
    {
        [BsonElement("fileId")]
        public required ObjectId? FileId;

        [BsonElement("fileContentId")]
        public required ObjectId? FileContentId;

        [BsonElement("fileSnapshotId")]
        public required ObjectId? FileSnapshotId;

        [BsonElement("userId")]
        public required ObjectId? UserId;

        [BsonElement("pagination")]
        public required PaginationOptions? Pagination;
    }

    private sealed record class GetFileLogsResponse
    {
        [BsonElement("fileLogs")]
        public required string[] FileLogs;
    }

    private TransactedRequestHandler<GetFileLogsRequest, GetFileLogsResponse> GetFileLogs =>
        async (transaction, request) =>
        {
            UnlockedUserAuthentication userAuthentication = Internal_EnsureAuthentication();
            User me = await Internal_Me(transaction, userAuthentication);

            if (
                me.Id != request.UserId
                && !await Resources
                    .GetAdminAccesses(transaction, me.Id)
                    .ToAsyncEnumerable()
                    .AnyAsync(transaction)
            )
            {
                throw new InvalidOperationException(
                    "User ID other than self is not allowed when not an administrator."
                );
            }

            User? user =
                request.UserId != null
                    ? await Resources
                        .GetUsers(transaction, id: request.UserId)
                        .ToAsyncEnumerable()
                        .FirstOrDefaultAsync(transaction)
                    : null;

            File? file =
                request.FileId != null
                    ? await Internal_GetFile(transaction, me, request.FileId)
                    : null;

            FileAccessResult? fileAccessResult =
                file != null
                    ? await Resources.FindFileAccess(
                        transaction,
                        file,
                        me,
                        userAuthentication,
                        FileAccessLevel.ReadWrite
                    )
                    : null;

            if (
                file != null
                && fileAccessResult == null
                && !await Resources
                    .GetAdminAccesses(transaction, me.Id)
                    .ToAsyncEnumerable()
                    .AnyAsync(transaction)
            )
            {
                throw new InvalidOperationException(
                    "File access is required when not an administrator."
                );
            }

            FileContent? fileContent = null;
            if (request.FileContentId != null)
            {
                if (file == null)
                {
                    throw new InvalidOperationException(
                        "File ID is required when file content ID is provided."
                    );
                }

                fileContent =
                    await Resources
                        .GetFileContents(transaction, file, id: request.FileContentId)
                        .ToAsyncEnumerable()
                        .FirstOrDefaultAsync(transaction.CancellationToken)
                    ?? throw new InvalidOperationException("File content not found.");
            }

            FileSnapshot? fileSnapshot = null;

            if (request.FileSnapshotId != null)
            {
                if (fileContent == null)
                {
                    throw new InvalidOperationException(
                        "File content ID is required when file snapshot ID is provided."
                    );
                }

                fileSnapshot =
                    await Resources
                        .GetFileSnapshots(transaction, file!, fileContent, request.FileSnapshotId)
                        .ToAsyncEnumerable()
                        .FirstOrDefaultAsync(transaction.CancellationToken)
                    ?? throw new InvalidOperationException("File snapshot not found.");
            }

            FileLog[] fileLogs = await Resources
                .GetFileLogs(transaction, file, fileContent, fileSnapshot, user)
                .ApplyPagination(request.Pagination)
                .ToAsyncEnumerable()
                .ToArrayAsync(transaction.CancellationToken);

            return new() { FileLogs = fileLogs.Select(fileLog => fileLog.ToString()).ToArray() };
        };
}
