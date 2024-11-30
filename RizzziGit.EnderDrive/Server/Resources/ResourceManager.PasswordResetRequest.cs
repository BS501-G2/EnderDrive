using System.Threading.Tasks;
using MongoDB.Bson;
using Newtonsoft.Json;

namespace RizzziGit.EnderDrive.Server.Resources;

public record class PasswordResetRequest : ResourceData
{
  public required ObjectId UserId;
  public required PasswordResetRequestStatus Status;
}

public enum PasswordResetRequestStatus
{
  Pending,
  Accepted,
  Declined,
}

public sealed partial class ResourceManager
{
  public async Task<Resource<PasswordResetRequest>> CreatePasswordResetRequest(
    ResourceTransaction transaction,
    Resource<User> user
  )
  {
    Resource<PasswordResetRequest> request = ToResource<PasswordResetRequest>(
      transaction,
      new() { UserId = user.Id, Status = PasswordResetRequestStatus.Pending }
    );

    await request.Save(transaction);
    return request;
  }
}
