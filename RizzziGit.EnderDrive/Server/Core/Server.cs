using System.IO;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace RizzziGit.EnderDrive.Server.Core;

using Commons.Services;
using Resources;
using Services;

public sealed class ServerData
{
    public required ResourceManager ResourceManager;
    public required KeyManager KeyGenerator;
    public required VirusScanner VirusScanner;
    public required ApiServer ApiServer;
    public required GoogleService GoogleService;
    public required ConnectionManager ConnectionManager;
}

public sealed class Server(
    string workingPath,
    string clamAvSocketPath = "/run/clamav/clamd.ctl",
    int httpPort = 8082,
    int httpsPort = 8442
) : Service<ServerData>("Server")
{
    private string ServerFolder => Path.Join(workingPath, ".EnderDrive");

    protected override async Task<ServerData> OnStart(
        CancellationToken startupCancellationToken,
        CancellationToken serviceCancellationToken
    )
    {
        ResourceManager resourceManager = new(this);
        KeyManager keyGenerator = new(this);
        VirusScanner virusScanner = new(this, clamAvSocketPath);
        ApiServer apiServer = new(this, httpPort, httpsPort);
        GoogleService googleService = new(this);
        ConnectionManager connectionManager = new(this);

        await StartServices(
            [keyGenerator, virusScanner, resourceManager, apiServer, googleService, connectionManager],
            startupCancellationToken
        );

        return new()
        {
            ResourceManager = resourceManager,
            KeyGenerator = keyGenerator,
            VirusScanner = virusScanner,
            ApiServer = apiServer,
            GoogleService = googleService,
            ConnectionManager = connectionManager
        };
    }

    public ResourceManager ResourceManager => GetContext().ResourceManager;
    public KeyManager KeyManager => GetContext().KeyGenerator;
    public VirusScanner VirusScanner => GetContext().VirusScanner;
    public ApiServer ApiServer => GetContext().ApiServer;
    public GoogleService GoogleService => GetContext().GoogleService;
    public ConnectionManager ConnectionManager => GetContext().ConnectionManager;

    public new Task Start(CancellationToken cancellationToken = default) =>
        base.Start(cancellationToken);

    protected override async Task OnRun(ServerData data, CancellationToken cancellationToken)
    {
        ServerData context = GetContext();

        await await Task.WhenAny(
            WatchService(context.KeyGenerator, cancellationToken),
            WatchService(context.VirusScanner, cancellationToken),
            WatchService(context.ResourceManager, cancellationToken),
            WatchService(context.ApiServer, cancellationToken),
            WatchService(context.GoogleService, cancellationToken),
            WatchService(context.ConnectionManager, cancellationToken)
        );
    }

    protected override async Task OnStop(ServerData data, ExceptionDispatchInfo? exception)
    {
        ServerData context = GetContext();

        await StopServices(
            context.ConnectionManager,
            context.GoogleService,
            context.ApiServer,
            context.ResourceManager,
            context.VirusScanner,
            context.KeyGenerator
        );
    }
}
