using System.IO;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

using System;
using Resources;

public sealed partial class Connection
{
  private sealed record class SetFileStarRequest : BaseFileRequest
  {
    public required bool Starred;
  }

  private sealed record class SetFileStarResponse { }

  private FileRequestHandler<SetFileStarRequest, SetFileStarResponse> SetFileStar =>
    async (transaction, request, userAuthentication, me, _, fileAccess) =>
    {
      Resource<FileStar> resource = await Resources.GetFileStar(
        transaction,
        fileAccess.UnlockedFile.File,
        me
      );

      resource.Data.Starred = request.Starred;

      await resource.Save(transaction);
      return new() { };
    };
}
