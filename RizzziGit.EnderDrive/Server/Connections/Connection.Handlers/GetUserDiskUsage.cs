using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using RizzziGit.EnderDrive.Server.Resources;

namespace RizzziGit.EnderDrive.Server.Connections;

public sealed partial class Connection
{
  private sealed record GetUserDiskUsageRequest
  {
    public required ObjectId? BaseDirectory;
    public required ObjectId? UserId;
  }

  private sealed record GetUserDiskUsageResponse
  {
    public required long FileCount;
    public required long DiskUsage;
  }

  private AuthenticatedRequestHandler<
    GetUserDiskUsageRequest,
    GetUserDiskUsageResponse
  > GetUserDiskUsage =>
    async (transaction, request, userAuthentication, me, myAdminAccess) =>
    {
      Resource<User> user =
        request.UserId != null
          ? await Internal_EnsureFirst<User>(
            transaction,
            query => query.Where((user) => user.Id == request.UserId)
          )
          : me;

      if (user.Id != me.Id && myAdminAccess == null)
      {
        throw new InvalidOperationException("User ID must be set to self when not an admin.");
      }

      Resource<File>? file =
        request.BaseDirectory != null
          ? await Internal_EnsureFirst<File>(
            transaction,
            (query) => query.Where((file) => file.Id == request.BaseDirectory)
          )
          : await Internal_GetFirst(
            transaction,
            Resources.Query<File>(
              transaction,
              (query) => query.Where((file) => file.OwnerUserId == request.UserId)
            )
          );

      async IAsyncEnumerable<Resource<File>> enumerate(Resource<File> parent)
      {
        await foreach (
          Resource<File> file in Resources.Query<File>(
            transaction,
            (query) => query.Where((file) => file.ParentId == parent.Id)
          )
        )
        {
          yield return file;

          if (file.Data.Type == FileType.Folder)
          {
            await foreach (Resource<File> innerFile in enumerate(file))
            {
              yield return innerFile;
            }
          }
        }
      }

      GetUserDiskUsageResponse response = new() { FileCount = 0, DiskUsage = 0 };

      if (file != null)
      {
        await enumerate(file)
          .AggregateAwaitAsync(
            response,
            async (response, file) =>
            {
              response.FileCount += 1;

              if (file.Data.Type == FileType.File)
              {
                response.DiskUsage = await Resources
                  .Query<FileData>(
                    transaction,
                    query => query.Where((fileData) => fileData.FileId == file.Id)
                  )
                  .AggregateAsync(
                    response.DiskUsage,
                    (diskUsage, fileData) =>
                      fileData.Data.SegmentsIds.Aggregate(
                        diskUsage,
                        (diskUsage, segment) => segment.Size + diskUsage
                      )
                  );
              }

              return response;
            }
          );
      }

      return response;
    };
}
