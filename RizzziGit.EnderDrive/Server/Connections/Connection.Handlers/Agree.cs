namespace RizzziGit.EnderDrive.Server.Connections;

public sealed partial class Connection
{
  private sealed record class AgreeRequest { }

  private sealed record class AgreeResponse { }

  private AuthenticatedRequestHandler<AgreeRequest, AgreeResponse> Agree =>
    async (transaction, request, userAuthentication, me, myAdminAccess) =>
    {
      me.Data.PrivacyPolicyAgreement = true;
      await me.Save(transaction);

      return new() { };
    };
}
