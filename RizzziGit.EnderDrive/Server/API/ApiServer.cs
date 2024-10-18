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

namespace RizzziGit.EnderDrive.Server.API;

using Commons.Memory;
using Commons.Services;
using Core;
using Services;

public sealed partial class ApiServerParams
{
    public required WebApplication WebApplication;
    public required SessionManager SessionManager;
    public required SocketIoBridge SocketIoBridge;
}

public sealed partial class ApiServer(Server server, int httpPort, int httpsPort)
    : Service2<ApiServerParams>("API", server)
{
    protected override async Task<ApiServerParams> OnStart(CancellationToken cancellationToken)
    {
        SessionManager sessionManager = new(this);
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

        await StartServices([sessionManager, socketIoBridge], cancellationToken);
        app.Use((HttpContext context, Func<Task> next) => Handle(context, cancellationToken));

        await app.StartAsync(cancellationToken);

        return new()
        {
            WebApplication = app,
            SessionManager = sessionManager,
            SocketIoBridge = socketIoBridge
        };
    }

    protected override async Task OnRun(ApiServerParams data, CancellationToken cancellationToken)
    {
        await await Task.WhenAny(
            WatchService(Context.SocketIoBridge, cancellationToken),
            WatchService(Context.SessionManager, cancellationToken)
        );
    }

    protected override async Task OnStop(ApiServerParams data, Exception? exception)
    {
        await Context.WebApplication.StopAsync(CancellationToken.None);

        await StopServices(Context.SocketIoBridge, Context.SessionManager);
    }

    private async Task Handle(HttpContext context, CancellationToken cancellationToken)
    {
        if (context.WebSockets.IsWebSocketRequest && context.Request.Path == "/ws")
        {
            HandleWebSocket(await context.WebSockets.AcceptWebSocketAsync(), cancellationToken);
        }
        else if ($"{context.Request.Path}".StartsWith("/res"))
        {
            await HandleWebRequest(context, cancellationToken);
        }
        else
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await context.Response.CompleteAsync();
        }
    }

    private async void HandleWebSocket(WebSocket webSocket, CancellationToken cancellationToken)
    {
        Connection connection = await server.ConnectionManager.Connect(
            webSocket,
            cancellationToken
        );

        try
        {
            await WatchService(connection, cancellationToken);
        }
        catch { }
    }

    private async Task HandleWebRequest(HttpContext context, CancellationToken cancellationToken)
    {
        context.Response.StatusCode = (int)HttpStatusCode.NotFound;
        await context.Response.CompleteAsync();
    }
}
