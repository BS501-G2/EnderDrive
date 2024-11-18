using System.Threading.Tasks;

namespace RizzziGit.EnderDrive.Server.Connections;

using Resources;

public sealed partial class Connection
{
  private delegate Task<R> TransactedRequestHandler<S, R>(
    ResourceTransaction transaction,
    S request
  );

  private void RegisterTransactedHandler<S, R>(
    ConnectionContext context,
    ServerSideRequestCode code,
    TransactedRequestHandler<S, R> handler
  ) =>
    RegisterHandler<S, R>(
      context,
      code,
      (request, cancellationToken) =>
        Resources.Transact((transaction) => handler(transaction, request), cancellationToken)
    );
}
