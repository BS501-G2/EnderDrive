using System;
using System.IO;
using System.IO.Pipes;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ClamAV.Net.Client;
using ClamAV.Net.Client.Results;

namespace RizzziGit.EnderDrive.Server.Services;

using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using Commons.Collections;
using Commons.Services;
using Core;
using MongoDB.Bson;
using RizzziGit.EnderDrive.Server.Resources;
using Utilities;

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

public sealed partial class VirusScanner(
  Server server,
  string unixSocketPath
)
  : Service<VirusScannerContext>(
    "Virus Scanner",
    server
  )
{
  protected override async Task<VirusScannerContext> OnStart(
    CancellationToken startupCancellationToken,
    CancellationToken serviceCancellationToken
  )
  {
    (
      IPEndPoint ipEndPoint,
      TcpForwarder tcpForwarder
    ) =
      await StartTcpForwarder(
        startupCancellationToken
      );

    {
      using IClamAvClient client =
        ClamAvClient.Create(
          new(
            $"tcp://{ipEndPoint}"
          )
        );
      VersionResult version =
        await client.GetVersionAsync(
          startupCancellationToken
        );

      Info(
        version.ProgramVersion,
        "Version"
      );
      Info(
        $"Virus Database {version.VirusDbVersion}",
        "Version"
      );
    }

    return new()
    {
      IPEndPoint =
        ipEndPoint,
      WaitQueue =
        new(),
      TcpForwarder =
        tcpForwarder,
    };
  }

  private async Task<(
    IPEndPoint ipEndPoint,
    TcpForwarder tcpForwarder
  )> StartTcpForwarder(
    CancellationToken cancellationToken
  )
  {
    while (
      true
    )
    {
      try
      {
        IPEndPoint ipEndPoint =
          new(
            IPAddress.Loopback,
            Random.Shared.Next(
              1025,
              65535
            )
          );
        TcpForwarder tcpForwarder =
          new(
            this,
            ipEndPoint,
            unixSocketPath
          );

        await StartServices(
          [
            tcpForwarder,
          ],
          cancellationToken
        );

        return (
          ipEndPoint,
          tcpForwarder
        );
      }
      catch (Exception exception)
      {
        Error(
          exception
        );
      }
    }
  }

  private async Task RunScanQueue(
    CancellationToken serviceCancellationToken
  )
  {
    while (
      true
    )
    {
      serviceCancellationToken.ThrowIfCancellationRequested();
      VirusScannerContext context =
        GetContext();

      await foreach (
        var (
          source,
          stream,
          cancellationToken
        ) in context.WaitQueue.WithCancellation(
          serviceCancellationToken
        )
      )
      {
        Debug(
          $"Received Scan Request."
        );

        using CancellationTokenSource linked =
          CancellationTokenSource.CreateLinkedTokenSource(
            serviceCancellationToken,
            cancellationToken
          );

        try
        {
          using IClamAvClient client =
            ClamAvClient.Create(
              new(
                $"tcp://{context.IPEndPoint}"
              )
            );
          ScanResult result =
            await client.ScanDataAsync(
              stream,
              linked.Token
            );

          source.SetResult(
            result
          );

          Debug(
            $"Scan Request Completed."
          );
        }
        catch (Exception exception)
        {
          source.SetException(
            ExceptionDispatchInfo.SetCurrentStackTrace(
              new AggregateException(
                "Virus scanner has failed.",
                exception
              )
            )
          );

          Error(
            exception
          );
        }
      }
    }
  }

  protected override async Task OnRun(
    VirusScannerContext data,
    CancellationToken cancellationToken
  )
  {
    await Task.WhenAll(
      [
        RunScanQueue(
          cancellationToken
        ),
        data.TcpForwarder.Watch(
          cancellationToken
        ),
      ]
    );
  }

  protected override async Task OnStop(
    VirusScannerContext data,
    ExceptionDispatchInfo? exception
  )
  {
    await StopServices(
      [
        data.TcpForwarder,
      ]
    );
  }

  public async Task<ScanResult> Scan(
    Stream stream,
    CancellationToken cancellationToken =
      default
  )
  {
    VirusScannerContext context =
      GetContext();

    TaskCompletionSource<ScanResult> source =
      new();
    await context.WaitQueue.Enqueue(
      (
        source,
        stream,
        cancellationToken
      ),
      cancellationToken
    );
    return await source.Task;
  }

  public async Task<VirusReport> Scan(
    ResourceTransaction transaction,
    UnlockedFile file,
    FileContent fileContent,
    FileSnapshot fileSnapshot,
    bool forceRescan
  )
  {
    VirusReport? virusReport =
      await server.ResourceManager.GetVirusReport(
        transaction,
        file,
        fileContent,
        fileSnapshot
      );

    if (
      forceRescan
      || virusReport
        == null
    )
    {
      try
      {
        using Stream stream =
          await server.ResourceManager.CreateReadStream(
            transaction,
            file,
            fileContent,
            fileSnapshot
          );

        ScanResult result =
          await Scan(
            stream,
            transaction.CancellationToken
          );

        virusReport =
          await server.ResourceManager.SetVirusReport(
            transaction,
            file,
            fileContent,
            fileSnapshot,
            VirusReportStatus.Completed,
            result.VirusName
            != null
              ?
              [
                result.VirusName,
              ]
              :
              []
          );
      }
      catch
      {
        virusReport =
          await server.ResourceManager.SetVirusReport(
            transaction,
            file,
            fileContent,
            fileSnapshot,
            VirusReportStatus.Failed,
            []
          );
      }
    }

    return virusReport;
  }
}
