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

public sealed class VirusScannerParams
{
    public required IClamAvClient Client;
    public required WaitQueue<(
        TaskCompletionSource<ScanResult> Source,
        Stream Stream,
        CancellationToken CancellationToken
    )> WaitQueue;
}

public sealed partial class VirusScanner(Server server, string unixSocketPath)
    : Service<VirusScannerParams>("Virus Scanner", server)
{
    protected override async Task<VirusScannerParams> OnStart(
        CancellationToken startupCancellationToken,
        CancellationToken serviceCancellationToken
    )
    {
        (IPEndPoint ipEndPoint, _) = await StartTcpForwarder(startupCancellationToken);

        IClamAvClient client = ClamAvClient.Create(new($"tcp://{ipEndPoint}"));

        await client.PingAsync(startupCancellationToken);

        VersionResult version = await client.GetVersionAsync(startupCancellationToken);

        Info(version.ProgramVersion, "Version");
        Info($"Virus Database {version.VirusDbVersion}", "Version");

        return new() { Client = client, WaitQueue = new() };
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
            VirusScannerParams context = GetContext();

            await foreach (
                var (source, stream, cancellationToken) in context.WaitQueue.WithCancellation(
                    serviceCancellationToken
                )
            )
            {
                Debug($"Received Scan Request.");

                using CancellationTokenSource linked =
                    CancellationTokenSource.CreateLinkedTokenSource(
                        serviceCancellationToken,
                        cancellationToken
                    );

                try
                {
                    ScanResult result = await context.Client.ScanDataAsync(stream, linked.Token);

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
            }
        }
    }

    protected override async Task OnRun(
        VirusScannerParams data,
        CancellationToken cancellationToken
    )
    {
        await Task.WhenAll([RunScanQueue(cancellationToken)]);
    }

    protected override Task OnStop(VirusScannerParams data, ExceptionDispatchInfo? exception)
    {
        VirusScannerParams context = GetContext();

        context.Client.Dispose();
        return Task.CompletedTask;
    }

    public async Task<ScanResult> Scan(Stream stream, CancellationToken cancellationToken = default)
    {
        VirusScannerParams context = GetContext();

        TaskCompletionSource<ScanResult> source = new();
        await context.WaitQueue.Enqueue((source, stream, cancellationToken), cancellationToken);
        return await source.Task;
    }

    public async Task<VirusReport> Scan(
        ResourceTransaction transaction,
        UnlockedFile file,
        FileContent fileContent,
        FileSnapshot fileSnapshot,
        bool forceRescan,
        CancellationToken cancellationToken = default
    )
    {
        VirusReport? virusReport = await server.ResourceManager.GetVirusReport(
            transaction,
            file,
            fileContent,
            fileSnapshot
        );

        if (forceRescan || virusReport == null)
        {
            try
            {
                using Stream stream = await server.ResourceManager.CreateReadStream(
                    transaction,
                    file,
                    fileContent,
                    fileSnapshot
                );

                ScanResult result = await Scan(stream, cancellationToken);

                virusReport = await server.ResourceManager.SetVirusReport(
                    transaction,
                    file,
                    fileContent,
                    fileSnapshot,
                    VirusReportStatus.Completed,
                    result.VirusName != null ? [result.VirusName] : []
                );
            }
            catch
            {
                virusReport = await server.ResourceManager.SetVirusReport(
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
