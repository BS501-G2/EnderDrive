using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

public sealed partial class Connection
{
  private sealed record class DidIAgreeRequest { }

  private sealed record class DidIAgreeResponse
  {
    public required bool Agreed;
  }

  private AuthenticatedRequestHandler<DidIAgreeRequest, DidIAgreeResponse> DidIAgree =>
    async (transaction, request, userAuthentication, me, myAdminAccess) =>
      new() { Agreed = me.Data.PrivacyPolicyAgreement };
}
