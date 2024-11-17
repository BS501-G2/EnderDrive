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
    : BaseFileRequest
  {
    [BsonElement(
      "pagination"
    )]
    public required PaginationOptions? Pagination;
  }

  private sealed record class GetFilePathResponse
  {
    [BsonElement(
      "path"
    )]
    public required string[] Path;
  }

  private FileRequestHandler<
    GetFilePathRequest,
    GetFilePathResponse
  > GetFilePath =>
    async (
      transaction,
      request,
      userAuthentication,
      me,
      _,
      currentFile,
      result
    ) =>
    {
      File rootFile =
        result.FileAccess
        != null
          ? result.File
          : await Resources.GetRootFolder(
            transaction,
            me
          );

      List<File> path =

        [
          currentFile,
        ];
      while (
        true
      )
      {
        currentFile =
          await Internal_GetFile(
            transaction,
            me,
            currentFile.ParentId
          );

        if (
          path.Last().Id
          != currentFile.Id
        )
        {
          path.Add(
            currentFile
          );
        }

        if (
          currentFile.Id
          == rootFile.Id
        )
        {
          break;
        }
      }

      return new()
      {
        Path =
          path.Reverse<File>()
            .Select(
              (
                entry
              ) =>
                JToken
                  .FromObject(
                    entry
                  )
                  .ToString()
            )
            .ToArray(),
      };
    };
}
