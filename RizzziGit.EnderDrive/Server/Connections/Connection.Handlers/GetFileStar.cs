using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using RizzziGit.EnderDrive.Server.Resources;

namespace RizzziGit.EnderDrive.Server.Connections;

public sealed partial class Connection
{
  private sealed record class GetFileStarRequest : BaseFileRequest { }

  private sealed record class GetFileStarResponse
  {
    public required bool Starred;
  }

  private FileRequestHandler<GetFileStarRequest, GetFileStarResponse> GetFileStar =>
    async (transaction, request, userAuthentication, me, myAdminAccess, fileAccess) =>
    {
      return new()
      {
        Starred = (await Resources.GetFileStar(transaction, fileAccess.UnlockedFile.File, me))
          .Data
          .Starred,
      };
    };
}
