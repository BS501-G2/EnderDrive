using System.Threading.Tasks;
using MongoDB.Bson;

namespace RizzziGit.EnderDrive.Server.Resources;

public record class PasswordResetRequest : ResourceData
{
  public required ObjectId UserId;
}

public sealed partial class ResourceManager
{
  public async Task<Resource<PasswordResetRequest>> CreatePasswordResetRequest(
    ResourceTransaction transaction,
    User user
  )
  {
    Resource<PasswordResetRequest> request = ToResource<PasswordResetRequest>(
      transaction,
      new() { UserId = user.Id }
    );

    await request.Save(transaction);
    return request;
  }
}
