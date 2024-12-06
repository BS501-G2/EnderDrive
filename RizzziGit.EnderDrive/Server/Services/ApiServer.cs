using System;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace RizzziGit.EnderDrive.Server.Services;

using System.Runtime.ExceptionServices;
using Commons.Memory;
using Commons.Services;
using Core;
using RizzziGit.EnderDrive.Utilities;
using Services;

public sealed partial class ApiServerContext
{
  public required WebApplication WebApplication;
  public required SocketIoBridge SocketIoBridge;
}

public sealed partial class ApiServer(EnderDriveServer server, int httpPort, int httpsPort)
  : Service<ApiServerContext>("API", server)
{
  public EnderDriveServer Server => server;

  protected override async Task<ApiServerContext> OnStart(
    CancellationToken startupCancellationToken,
    CancellationToken serviceCancellationToken
  )
  {
    SocketIoBridge socketIoBridge = new(this);

    WebApplicationBuilder builder = WebApplication.CreateBuilder();

    builder.Logging.ClearProviders();
    builder.WebHost.ConfigureKestrel(
      (context, options) =>
      {
        options.Listen(
          IPAddress.Any,
          httpPort,
          (options) =>
          {
            options.Protocols = HttpProtocols.Http1AndHttp2;
          }
        );

        options.Listen(
          IPAddress.Any,
          httpsPort,
          (options) =>
          {
            options.Protocols = HttpProtocols.Http1AndHttp2AndHttp3;
            options.UseHttps();
          }
        );
      }
    );

    builder.Services.AddRazorComponents().AddInteractiveServerComponents();

    WebApplication app = builder.Build();

    app.UseWebSockets(new() { KeepAliveInterval = TimeSpan.FromMinutes(2) });

    await StartServices([socketIoBridge], startupCancellationToken);
    app.Use((HttpContext context, Func<Task> _) => Handle(context, serviceCancellationToken));

    await app.StartAsync(startupCancellationToken);

    return new() { WebApplication = app, SocketIoBridge = socketIoBridge };
  }

  protected override async Task OnRun(ApiServerContext context, CancellationToken cancellationToken)
  {
    Task[] tasks = [RunBridgeLoop(context, cancellationToken)];

    await Task.WhenAny(tasks);
  }

  private async Task RunBridgeLoop(ApiServerContext context, CancellationToken cancellationToken)
  {
    while (true)
    {
      cancellationToken.ThrowIfCancellationRequested();

      try
      {
        await WatchService(context.SocketIoBridge, cancellationToken);
      }
      catch (Exception exception)
      {
        Error(exception);
      }

      try
      {
        await context.SocketIoBridge.Stop();
      }
      catch { }

      try
      {
        await (context.SocketIoBridge  = new(this)).Start(cancellationToken);
      }
      catch { }
    }
  }

  protected override async Task OnStop(ApiServerContext data, ExceptionDispatchInfo? exception)
  {
    var context = GetContext();
    await context.WebApplication.StopAsync(CancellationToken.None);

    await StopServices(context.SocketIoBridge);
  }

  private async Task Handle(HttpContext context, CancellationToken cancellationToken)
  {
    if (!context.WebSockets.IsWebSocketRequest)
    {
      context.Response.StatusCode = 400;
      await context.Response.CompleteAsync();
      return;
    }

    await server.ConnectionManager.Push(
      await context.WebSockets.AcceptWebSocketAsync(),
      cancellationToken
    );
  }
}
