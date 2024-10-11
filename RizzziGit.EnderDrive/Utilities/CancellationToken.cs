using System.Linq;
using System.Threading;

namespace RizzziGit.EnderDrive.Utilities;

public static class CancellationTokenExtensions
{
    public static CancellationTokenSource Link(
        this CancellationToken cancellationToken,
        params CancellationToken?[] cancellationTokens
    ) =>
        CancellationTokenSource.CreateLinkedTokenSource(
            [
                cancellationToken,
                .. cancellationTokens.Where(x => x is not null).Select(x => (CancellationToken)x!)
            ]
        );
}
