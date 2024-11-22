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
  private sealed record class GetFilePathRequest : BaseFileRequest
  {
    [BsonElement("pagination")]
    public required PaginationOptions? Pagination;
  }

  private sealed record class GetFilePathResponse
  {
    [BsonElement("path")]
    public required string[] Path;
  }

  private FileRequestHandler<GetFilePathRequest, GetFilePathResponse> GetFilePath =>
    async (transaction, request, userAuthentication, me, _, result) =>
    {
      UnlockedFile rootFile =
        result.FileAccess != null
          ? result.UnlockedFile
          : await Resources.GetRootFolder(transaction, me, userAuthentication);

      Resource<File> currentFile = result.UnlockedFile.File;
      List<Resource<File>> path = [];

      while (true)
      {
        FileAccessResult? fileAccess = await Resources.FindFileAccess(
          transaction,
          currentFile,
          me,
          userAuthentication,
          FileAccessLevel.Read
        );

        if (fileAccess == null)
        {
          break;
        }

        path.Add(currentFile);

        if (currentFile.Data.ParentId == null)
        {
          break;
        }

        currentFile = await Internal_GetFile(
          transaction,
          me,
          userAuthentication,
          currentFile.Data.ParentId
        );
      }

      return new() { Path = [.. path.Reverse<Resource<File>>().ToJson()] };
    };
}
