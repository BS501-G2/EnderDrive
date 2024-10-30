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
    public required VirusScanner.TcpForwarder TcpForwarder;
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
        CancellationToken cancellationToken,
        CancellationToken a
    )
    {
        IPEndPoint ipEndPoint = new(IPAddress.Loopback, Random.Shared.Next(1025, 65535));
        TcpForwarder tcpForwarder = new(this, ipEndPoint, unixSocketPath);

        await StartServices([tcpForwarder], cancellationToken);

        IClamAvClient client = ClamAvClient.Create(new($"tcp://{ipEndPoint}"));

        await client.PingAsync(cancellationToken);

        VersionResult version = await client.GetVersionAsync(cancellationToken);

        Info(version.ProgramVersion, "Version");
        Info($"Virus Database {version.VirusDbVersion}", "Version");

        return new()
        {
            Client = client,
            TcpForwarder = tcpForwarder,
            WaitQueue = new(),
        };
    }

    private async Task RunScanQueue(CancellationToken serviceCancellationToken)
    {
        while (true)
        {
            serviceCancellationToken.ThrowIfCancellationRequested();

            await foreach (
                var (source, stream, cancellationToken) in Context.WaitQueue.WithCancellation(
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
                    ScanResult result = await Context.Client.ScanDataAsync(stream, linked.Token);

                    source.SetResult(result);

                    Debug($"Scan Request Completed.");
                }
                catch (Exception exception)
                {
                    source.SetException(exception);

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
        await Task.WhenAll([RunScanQueue(cancellationToken),]);
    }

    protected override Task OnStop(VirusScannerParams data, ExceptionDispatchInfo? exception)
    {
        Context.Client.Dispose();
        return Task.CompletedTask;
    }

    public async Task<ScanResult> Scan(Stream stream, CancellationToken cancellationToken = default)
    {
        TaskCompletionSource<ScanResult> source = new();

        await Context.WaitQueue.Enqueue((source, stream, cancellationToken), cancellationToken);
        return await source.Task;
    }

    public async Task<string[]> Scan(
        ResourceTransaction transaction,
        UnlockedFile file,
        FileContent fileContent,
        FileSnapshot fileSnapshot,
        CancellationToken cancellationToken = default
    )
    {
        VirusReport? virusReport = await server.ResourceManager.GetVirusReport(
            transaction,
            file,
            fileContent,
            fileSnapshot
        );

        if (virusReport == null)
        {
            using Stream stream = await server.ResourceManager.CreateReadStream(
                transaction,
                file,
                fileContent,
                fileSnapshot
            );

            virusReport = await server.ResourceManager.SetVirusReport(
                transaction,
                file,
                fileContent,
                fileSnapshot,
                [(await Scan(stream, cancellationToken)).VirusName]
            );
        }

        return virusReport.Viruses;
    }
}
