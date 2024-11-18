using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth;
using Google.Apis.Services;

namespace RizzziGit.EnderDrive.Server.Services;

using Commons.Collections;
using Commons.Services;
using Core;
using Resources;

public sealed record class GoogleContext
{
  public required WaitQueue<GoogleService.Feed> Feed;
}

public sealed partial class GoogleService(Server server)
  : Service<GoogleContext>("Google API", server)
{
  public Server Server => server;
  public ResourceManager Resources => Server.ResourceManager;

  public abstract record Feed
  {
    private Feed() { }

    public sealed record GetPayload(
      TaskCompletionSource<byte[]> TaskCompletionSource,
      string Token
    ) : Feed();
  }

  protected override Task<GoogleContext> OnStart(
    CancellationToken startupCancellationToken,
    CancellationToken serviceCancellationToken
  )
  {
    BaseClientService.Initializer baseClientService =
      new() { ApiKey = "", ApplicationName = "EnderDrive" };

    return Task.FromResult<GoogleContext>(new() { Feed = new() });
  }

  protected override async Task OnRun(
    GoogleContext context,
    CancellationToken serviceCancellationToken
  )
  {
    await foreach (
      Feed feed in context.Feed.WithCancellation(serviceCancellationToken)
    )
    {
      switch (feed)
      {
        case Feed.GetPayload(
          TaskCompletionSource<byte[]> taskCompletionSource,
          string token
        ):
        {
          try
          {
            GoogleJsonWebSignature.Payload payload =
              await GoogleJsonWebSignature.ValidateAsync(token, new() { });

            taskCompletionSource.SetResult(Encoding.UTF8.GetBytes(payload.Prn));
          }
          catch (Exception exception)
          {
            taskCompletionSource.SetException(exception);
          }

          break;
        }
      }
    }
  }

  public async Task<byte[]> GetPayload(
    string token,
    CancellationToken cancellationToken
  )
  {
    TaskCompletionSource<byte[]> taskCompletionSource = new();

    await GetContext()
      .Feed.Enqueue(
        new Feed.GetPayload(taskCompletionSource, token),
        cancellationToken
      );

    return await taskCompletionSource.Task;
  }
}
