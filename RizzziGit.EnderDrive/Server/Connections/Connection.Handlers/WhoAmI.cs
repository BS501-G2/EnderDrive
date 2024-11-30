
using Newtonsoft.Json.Linq;

namespace RizzziGit.EnderDrive.Server.Connections;

public sealed partial class Connection
{
  private sealed record class MeRequest() { };

  private sealed record class MeResponse
  {
    public required string User;
  }

  private AuthenticatedRequestHandler<MeRequest, MeResponse> Me =>
    async (_, _, _, me, _) => new() { User = me.ToJson() };
}
