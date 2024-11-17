using System.Threading;
using System.Threading.Tasks;
using RizzziGit.Commons.Services;

namespace RizzziGit.EnderDrive.Server.Services;

using System;
using Core;
using RizzziGit.EnderDrive.Server.Resources;

public sealed record class AdminManagerContext
{
  public required UnlockedAdminKey UnlockedAdminKey;
}

public sealed partial class AdminManager(
  ResourceManager resources
)
  : Service<AdminManagerContext>(
    "Admin Manager",
    resources
  )
{
  protected override async Task<AdminManagerContext> OnStart(
    CancellationToken startupCancellationToken,
    CancellationToken serviceCancellationToken
  )
  {
    AdminKey? adminKey =
      await resources.Transact(
        resources.GetExistingAdminKey
      );

    UnlockedAdminKey unlockedAdminKey;
    {
      if (
        adminKey
        == null
      )
      {
        Console.Write(
          "Please enter a new system password: "
        );
        string? newPassword =
          Console.ReadLine();

        Console.Write(
          "Please confirm the new system password: "
        );
        string? confirmPassword =
          Console.ReadLine();

        if (
          newPassword
          != confirmPassword
        )
        {
          throw new InvalidOperationException(
            "Password mismatch"
          );
        }
        else if (
          newPassword
          == null
        )
        {
          throw new InvalidOperationException(
            "Password must be set."
          );
        }

        unlockedAdminKey =
          await resources.Transact(
            (
              transaction
            ) =>
              resources.CreateAdminKey(
                transaction,
                newPassword
              )
          );
      }
      else
      {
        Console.Write(
          "Please enter the existing system password: "
        );
        string? password =
          Console.ReadLine()
          ?? throw new InvalidOperationException(
            "Password must be set."
          );
        unlockedAdminKey =
          adminKey.Unlock(
            password
          );
      }
    }

    return new()
    {
      UnlockedAdminKey =
        unlockedAdminKey,
    };
  }

  public UnlockedAdminKey AdminKey =>
    GetContext().UnlockedAdminKey;
}
