using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MimeDetective.Storage;
using MongoDB.Bson;

namespace RizzziGit.EnderDrive.Server.Services;

using Commons.Services;
using Connections;
using Core;
using Newtonsoft.Json.Linq;
using Resources;

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
    app.Use(
      async (HttpContext context, Func<Task> _) =>
      {
        try
        {
          await Handle(context, serviceCancellationToken);
        }
        catch (Exception exception)
        {
          Error(exception);
        }
      }
    );

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
        Console.Error.WriteLine(exception);
        Error(exception);
      }

      try
      {
        await context.SocketIoBridge.Stop();
      }
      catch { }

      try
      {
        await (context.SocketIoBridge = new(this)).Start(cancellationToken);
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
    if (context.Request.Path.EntryEquals(1, "api"))
    {
      if (context.Request.Path.EntryEquals(2, "files"))
      {
        if (context.Request.Path.TryGetPathEntry(3, out string? tokenId))
        {
          await HandleFileRequest(context, ObjectId.Parse(tokenId), cancellationToken);
        }
        else
        {
          context.Response.StatusCode = 404;
        }
      }
      else
      {
        context.Response.StatusCode = 404;
      }
    }
    else if (context.WebSockets.IsWebSocketRequest)
    {
      await server.Connections.Push(
        await context.WebSockets.AcceptWebSocketAsync(),
        cancellationToken
      );

      return;
    }
    else
    {
      context.Response.StatusCode = 400;
    }

    await context.Response.CompleteAsync();
  }

  private async Task HandleFileRequest(
    HttpContext context,
    ObjectId tokenId,
    CancellationToken cancellationToken
  )
  {
    FileToken? fileToken = null;

    foreach (Connection connection in Server.Connections)
    {
      if (connection.TryGetFileToken(tokenId, out fileToken))
      {
        break;
      }
    }

    if (fileToken == null)
    {
      context.Response.StatusCode = 404;
      return;
    }

    RequestHeaders headers = context.Request.GetTypedHeaders();
    Definition? fileMime = await Resources.Transact(
      (transaction) => Server.MimeDetector.Inspect(transaction, fileToken.File, fileToken.FileData),
      cancellationToken
    );

    string getFileMimeType() =>
      (context.Request.Query.ContainsKey("download-mode") ? null : fileMime?.File.MimeType)
      ?? "application/octet-stream";

    long getSize() =>
      fileToken.FileData.Data.SegmentsIds.Aggregate(
        (long)0,
        (total, segment) => total + segment.Size
      );

    using ResourceManager.FileResourceStream stream = await Resources.Transact(
      (transaction) =>
        Resources.CreateFileStream(transaction, fileToken.File, fileToken.FileData, false),
      cancellationToken
    );

    context.Response.ContentType = getFileMimeType();

    if (context.Request.Query.ContainsKey("download-mode"))
    {
      context.Response.Headers.ContentDisposition =
        $"attachment; filename*=UTF-8''{Uri.EscapeDataString(fileToken.File.File.Data.Name)}";
    }

    if (headers.Range != null)
    {
      context.Response.StatusCode = 206;

      if (headers.Range.Ranges.Count == 1)
      {
        long start = headers.Range.Ranges.First().From ?? 0;
        long end = headers.Range.Ranges.First().To ?? (getSize());

        if (start > getSize() || end > getSize() || start < 0 || end < 0 || start > end)
        {
          context.Response.StatusCode = 416;
          return;
        }

        context.Response.Headers.ContentRange = $"bytes {start}-{end - 1}/{getSize()}";
        context.Response.ContentLength = end - start;

        await stream.SeekAsync(start, SeekOrigin.Begin, cancellationToken);
        long written = 0;

        while (written < (end - start))
        {
          byte[] buffer = new byte[256 * 1024];
          int bufferLength = await stream.ReadAsync(buffer, cancellationToken);

          if (bufferLength == 0)
          {
            break;
          }

          await context.Response.Body.WriteAsync(
            buffer.AsMemory(0, int.Min(bufferLength, (int)(end - written))),
            cancellationToken
          );

          written += bufferLength;
        }
      }
      else
      {
        context.Response.StatusCode = 416;
      }
    }
    else
    {
      context.Response.StatusCode = 200;
      context.Response.ContentLength = getSize();

      while (true)
      {
        byte[] buffer = new byte[256 * 1024];
        int bufferLength = await stream.ReadAsync(buffer, cancellationToken);

        if (bufferLength == 0)
        {
          break;
        }

        await context.Response.Body.WriteAsync(buffer.AsMemory(0, bufferLength), cancellationToken);
      }
    }
  }

  public ResourceManager Resources => Server.Resources;
}

public static class PathStringExtensions
{
  public static bool TryGetPathEntry(
    this PathString pathString,
    int index,
    [NotNullWhen(true)] out string? path
  )
  {
    path = null;

    string[] split = $"{pathString}".Split('/');
    if (split.Length <= index)
    {
      return false;
    }

    path = split[index];
    return true;
  }

  public static bool EntryEquals(
    this PathString pathString,
    int index,
    string value,
    StringComparison? comparison = null
  ) =>
    TryGetPathEntry(pathString, index, out string? path)
    && (
      comparison != null ? string.Equals(path, value, (StringComparison)comparison) : path == value
    );
}
