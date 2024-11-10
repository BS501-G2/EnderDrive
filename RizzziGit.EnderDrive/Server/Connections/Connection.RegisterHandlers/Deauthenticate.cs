using System.Threading.Tasks;

namespace RizzziGit.EnderDrive.Server.Connections;

public sealed partial class Connection
{
    private sealed record class DeauthenticateRequest { }

    private sealed record class DeauthenticateResponse { }

    private TransactedRequestHandler<
        DeauthenticateRequest,
        DeauthenticateResponse
    > Deauthenticate =>
        (transaction, request) =>
        {
            GetContext().CurrentUser = null;

            return Task.FromResult<DeauthenticateResponse>(new());
        };
}
