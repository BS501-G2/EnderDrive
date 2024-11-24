using System.Threading;
using System.Threading.Tasks;

namespace RizzziGit.EnderDrive.Client.API;

using Commons.Services;

public sealed class ApiClientContext { }

public sealed class ApiClient(IService downstream)
  : Service<ApiClientContext>("API Client", downstream)
{
  protected override async Task<ApiClientContext> OnStart(
    CancellationToken startupCancellationToken,
    CancellationToken serviceCancellationToken
  )
  {
    throw new System.NotImplementedException();
  }
}
