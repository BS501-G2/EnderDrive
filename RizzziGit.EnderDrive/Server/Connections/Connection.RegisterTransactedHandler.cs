using System.Threading.Tasks;

namespace RizzziGit.EnderDrive.Server.Connections;

using Resources;

public sealed partial class Connection
{
  private delegate Task<R> TransactedRequestHandler<S, R>(
    ResourceTransaction transaction,
    S request
  );

  private void RegisterTransactedRequestHandler<S, R>(
    string name,
    TransactedRequestHandler<S, R> handler
  ) =>
    RegisterRequestHandler<S, R>(
      name,
      (request, cancellationToken) =>
        Resources.Transact((transaction) => handler(transaction, request), cancellationToken)
    );
}
