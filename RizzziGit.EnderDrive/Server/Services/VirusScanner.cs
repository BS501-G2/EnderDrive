using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using ClamAV.Net.Client;
using ClamAV.Net.Client.Results;

namespace RizzziGit.EnderDrive.Server.Services;

using Commons.Collections;
using Commons.Services;
using Core;
using Resources;

public sealed class VirusScannerContext
{
  public required IPEndPoint IPEndPoint;
  public required WaitQueue<(
    TaskCompletionSource<ScanResult> Source,
    Stream Stream,
    CancellationToken CancellationToken
  )> WaitQueue;

  public required VirusScanner.TcpForwarder TcpForwarder;
}

public sealed partial class VirusScanner(Server server, string unixSocketPath)
  : Service<VirusScannerContext>("Virus Scanner", server)
{
  protected override async Task<VirusScannerContext> OnStart(
    CancellationToken startupCancellationToken,
    CancellationToken serviceCancellationToken
  )
  {
    (IPEndPoint ipEndPoint, TcpForwarder tcpForwarder) = await StartTcpForwarder(
      startupCancellationToken
    );

    {
      using IClamAvClient client = ClamAvClient.Create(new($"tcp://{ipEndPoint}"));
      VersionResult version = await client.GetVersionAsync(startupCancellationToken);

      Info(version.ProgramVersion, "Version");
      Info($"Virus Database {version.VirusDbVersion}", "Version");
    }

    return new()
    {
      IPEndPoint = ipEndPoint,
      WaitQueue = new(),
      TcpForwarder = tcpForwarder,
    };
  }

  private async Task<(IPEndPoint ipEndPoint, TcpForwarder tcpForwarder)> StartTcpForwarder(
    CancellationToken cancellationToken
  )
  {
    while (true)
    {
      try
      {
        IPEndPoint ipEndPoint = new(IPAddress.Loopback, Random.Shared.Next(1025, 65535));
        TcpForwarder tcpForwarder = new(this, ipEndPoint, unixSocketPath);

        await StartServices([tcpForwarder], cancellationToken);

        return (ipEndPoint, tcpForwarder);
      }
      catch (Exception exception)
      {
        Error(exception);
      }
    }
  }

  private async Task RunScanQueue(CancellationToken serviceCancellationToken)
  {
    while (true)
    {
      serviceCancellationToken.ThrowIfCancellationRequested();
      VirusScannerContext context = GetContext();

      await foreach (
        var (source, stream, cancellationToken) in context.WaitQueue.WithCancellation(
          serviceCancellationToken
        )
      )
      {
        Debug($"Received Scan Request.");

        _ = Task.Run(
          async () =>
          {
            using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(
              serviceCancellationToken,
              cancellationToken
            );

            try
            {
              using IClamAvClient client = ClamAvClient.Create(new($"tcp://{context.IPEndPoint}"));
              ScanResult result = await client.ScanDataAsync(stream, linked.Token);

              source.SetResult(result);

              Debug($"Scan Request Completed.");
            }
            catch (Exception exception)
            {
              source.SetException(
                ExceptionDispatchInfo.SetCurrentStackTrace(
                  new AggregateException("Virus scanner has failed.", exception)
                )
              );

              Error(exception);
            }
          },
          serviceCancellationToken
        );
      }
    }
  }

  protected override async Task OnRun(VirusScannerContext data, CancellationToken cancellationToken)
  {
    await Task.WhenAll(
      [RunScanQueue(cancellationToken), data.TcpForwarder.Watch(cancellationToken)]
    );
  }

  protected override async Task OnStop(VirusScannerContext data, ExceptionDispatchInfo? exception)
  {
    await StopServices([data.TcpForwarder]);
  }

  private async Task<ScanResult> Scan(Stream stream, CancellationToken cancellationToken = default)
  {
    VirusScannerContext context = GetContext();

    TaskCompletionSource<ScanResult> source = new();
    await context.WaitQueue.Enqueue((source, stream, cancellationToken), cancellationToken);
    return await source.Task;
  }

  public async Task<Resource<VirusReport>> Scan(
    ResourceTransaction transaction,
    UnlockedFile file,
    Resource<FileContent> fileContent,
    Resource<FileSnapshot> fileSnapshot,
    bool forceRescan
  )
  {
    Resource<VirusReport> virusReport = await server.Resources.GetVirusReport(
      transaction,
      file.File,
      fileContent,
      fileSnapshot
    );

    MimeDetective.Storage.Definition? definition = await server.MimeDetector.Inspect(
      transaction,
      file,
      fileContent,
      fileSnapshot
    );

    if (
      definition != null
      && definition.File.MimeType != null
      && (
        definition.File.MimeType.StartsWith("audio/")
        || definition.File.MimeType.StartsWith("video/")
        || definition.File.MimeType.StartsWith("image/")
        || definition.File.MimeType == "application/pdf"
        || definition.File.MimeType.StartsWith("font/")
      )
    )
    {
      virusReport.Data.Status = VirusReportStatus.Completed;
      virusReport.Data.Viruses = [];

      await virusReport.Save(transaction);
    }

    if (forceRescan || virusReport.Data.Status == VirusReportStatus.Pending)
    {
      try
      {
        using Stream stream = await server.Resources.CreateReadStream(
          transaction,
          file,
          fileContent,
          fileSnapshot
        );

        ScanResult result = await Scan(stream, transaction.CancellationToken);

        virusReport.Data.Status = VirusReportStatus.Completed;
        virusReport.Data.Viruses = result.VirusName != null ? [result.VirusName] : [];
      }
      catch (Exception exception)
      {
        Error(exception);
        virusReport.Data.Status = VirusReportStatus.Failed;
        virusReport.Data.Viruses = [];
      }

      await virusReport.Save(transaction);
    }

    return virusReport;
  }
}
