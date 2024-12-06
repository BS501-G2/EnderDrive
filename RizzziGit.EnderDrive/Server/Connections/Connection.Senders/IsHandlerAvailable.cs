using System.Threading.Tasks;

namespace RizzziGit.EnderDrive.Server.Connections;

public sealed partial class Connection
{
  private sealed record IsHandlerAvailableRequest
  {
    public required string Name;
  }

  private sealed record IsHandlerAvailableResponse
  {
    public required bool IsAvailable;
  }

  public async Task<bool> IsHandlerAvailable(string name)
  {
    return (
      await SendRequest<IsHandlerAvailableRequest, IsHandlerAvailableResponse>(
        nameof(IsHandlerAvailable),
        new() { Name = name }
      )
    ).IsAvailable;
  }
}
