using System.IO;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace RizzziGit.EnderDrive.Server.Core;

using Commons.Services;
using Connections;
using Resources;
using Services;

public sealed class ServerData
{
  public required ResourceManager ResourceManager;
  public required KeyManager KeyManager;
  public required VirusScanner VirusScanner;
  public required ApiServer ApiServer;
  public required GoogleService GoogleService;
  public required ConnectionManager ConnectionManager;
  public required MimeDetector MimeDetector;
  public required AdminManager AdminManager;
  public required AudioTranscriber AudioTranscriber;
}

public sealed class EnderDriveServer(
  string clamAvSocketPath = "/run/clamav/clamd.ctl",
  int httpPort = 8082,
  int httpsPort = 8442
) : Service<ServerData>("Server")
{
  protected override async Task<ServerData> OnStart(
    CancellationToken startupCancellationToken,
    CancellationToken serviceCancellationToken
  )
  {
    KeyManager keyManager = new(this);
    ResourceManager resourceManager = new(this);
    AdminManager adminManager = new(resourceManager);
    VirusScanner virusScanner = new(this, clamAvSocketPath);
    ApiServer apiServer = new(this, httpPort, httpsPort);
    GoogleService googleService = new(this);
    ConnectionManager connectionManager = new(this);
    MimeDetector mimeDetector = new(this);
    AudioTranscriber audioTranscriber = new(this, "/mnt/buffalo/bs701/model.bin");

    await StartServices([keyManager, resourceManager], startupCancellationToken);
    await StartServices([adminManager], startupCancellationToken);
    await StartServices(
      [virusScanner, apiServer, googleService, connectionManager, mimeDetector, audioTranscriber],
      startupCancellationToken
    );

    return new()
    {
      KeyManager = keyManager,
      ResourceManager = resourceManager,
      AdminManager = adminManager,
      VirusScanner = virusScanner,
      ApiServer = apiServer,
      GoogleService = googleService,
      ConnectionManager = connectionManager,
      MimeDetector = mimeDetector,
      AudioTranscriber = audioTranscriber
    };
  }

  public KeyManager KeyManager => GetContext().KeyManager;
  public ResourceManager Resources => GetContext().ResourceManager;
  public AdminManager AdminManager => GetContext().AdminManager;
  public VirusScanner VirusScanner => GetContext().VirusScanner;
  public ApiServer ApiServer => GetContext().ApiServer;
  public GoogleService GoogleService => GetContext().GoogleService;
  public ConnectionManager ConnectionManager => GetContext().ConnectionManager;
  public MimeDetector MimeDetector => GetContext().MimeDetector;
  public AudioTranscriber AudioTranscriber => GetContext().AudioTranscriber;

  public new Task Start(CancellationToken cancellationToken = default) =>
    base.Start(cancellationToken);

  protected override async Task OnRun(ServerData data, CancellationToken cancellationToken)
  {
    ServerData context = GetContext();

    await await Task.WhenAny(
      WatchService(context.KeyManager, cancellationToken),
      WatchService(context.ResourceManager, cancellationToken),
      WatchService(context.AdminManager, cancellationToken),
      WatchService(context.VirusScanner, cancellationToken),
      WatchService(context.ApiServer, cancellationToken),
      WatchService(context.GoogleService, cancellationToken),
      WatchService(context.ConnectionManager, cancellationToken),
      WatchService(context.MimeDetector, cancellationToken),
      WatchService(context.AudioTranscriber, cancellationToken)
    );
  }

  protected override async Task OnStop(ServerData data, ExceptionDispatchInfo? exception)
  {
    ServerData context = GetContext();

    await StopServices(
      context.AudioTranscriber,
      context.MimeDetector,
      context.ConnectionManager,
      context.GoogleService,
      context.ApiServer,
      context.VirusScanner,
      context.ResourceManager,
      context.AdminManager,
      context.KeyManager
    );
  }
}
