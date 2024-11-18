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

      Resource<File> currentFile = rootFile.File;
      List<Resource<File>> path = [rootFile.File];

      while (true)
      {
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

        if (path.Last().Id != currentFile.Id)
        {
          path.Add(currentFile);
        }

        if (currentFile.Id == rootFile.File.Data.Id)
        {
          break;
        }
      }

      return new() { Path = [.. path.Reverse<Resource<File>>().ToJson()] };
    };
}
