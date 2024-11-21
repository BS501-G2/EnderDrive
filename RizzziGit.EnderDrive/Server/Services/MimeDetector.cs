using System.Collections.Immutable;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using MimeDetective;
using MimeDetective.Definitions;
using MimeDetective.Definitions.Licensing;
using MimeDetective.Engine;
using MimeDetective.Storage;
using System.Linq;

namespace RizzziGit.EnderDrive.Server.Services;

using Commons.Collections;
using Commons.Services;
using Commons.Utilities;
using Core;
using Resources;

public sealed class MimeDetectorContext
{
  public required ContentInspector ContentInspector;
  public required WaitQueue<MimeDetectorRequest> WaitQueue;
  public required ImmutableArray<Definition> Definitions;
}

public sealed record MimeDetectorRequest(
  TaskCompletionSource<Definition?> Output,
  Stream Stream,
  CancellationToken CancellationToken
);

public sealed partial class MimeDetector(Server server)
  : Service<MimeDetectorContext>("Mime Detector", server)
{
  public Server Server => server;
  public ResourceManager Resources => Server.ResourceManager;

  protected override Task<MimeDetectorContext> OnStart(
    CancellationToken startupCancellationToken,
    CancellationToken serviceCancellationToken
  )
  {
    Info("Building definitions...");
    ImmutableArray<Definition> definitions = new ExhaustiveBuilder()
    {
      UsageType = UsageType.PersonalNonCommercial,
    }.Build();

    Info("Building content inspector...");
    ContentInspector contentInspector = new ContentInspectorBuilder()
    {
      Definitions = definitions,
      StringSegmentOptions = new() { OptimizeFor = StringSegmentResourceOptimization.HighSpeed },
    }.Build();

    Info("Initialization done!");
    return Task.FromResult<MimeDetectorContext>(
      new()
      {
        ContentInspector = contentInspector,
        WaitQueue = new(),
        Definitions = definitions,
      }
    );
  }

  protected override async Task OnRun(
    MimeDetectorContext context,
    CancellationToken serviceCancellationToken
  )
  {
    await foreach (
      (
        TaskCompletionSource<Definition?> output,
        Stream stream,
        CancellationToken cancellationToken
      ) in context.WaitQueue.WithCancellation(serviceCancellationToken)
    )
    {
      _ = Task.Run(
        async () =>
        {
          using CancellationTokenSource linked = serviceCancellationToken.Link(cancellationToken);

          DefinitionMatch? match = await context
            .ContentInspector.Inspect(stream)
            .OrderByDescending((match) => match.Percentage)
            .ToAsyncEnumerable()
            .FirstOrDefaultAsync(linked.Token);

          output.SetResult(match?.Definition);
        },
        cancellationToken
      );
    }
  }

  private async Task<Definition?> Inspect(Stream stream, CancellationToken cancellationToken)
  {
    MimeDetectorContext context = GetContext();
    TaskCompletionSource<Definition?> output = new();

    await context.WaitQueue.Enqueue(new(output, stream, cancellationToken), cancellationToken);
    return await output.Task;
  }

  public async Task<Definition?> Inspect(
    ResourceTransaction transaction,
    UnlockedFile file,
    Resource<FileContent> fileContent,
    Resource<FileSnapshot> fileSnapshot
  )
  {
    MimeDetectorContext context = GetContext();
    Resource<MimeDetectionReport> mimeDetectionReport = await Resources.GetMimeDetectionReport(
      transaction,
      file.File,
      fileContent,
      fileSnapshot
    );

    if (mimeDetectionReport.Data.Mime == null)
    {
      using Stream stream = await server.ResourceManager.CreateReadStream(
        transaction,
        file,
        fileContent,
        fileSnapshot
      );

      Definition? definition = await Inspect(stream, transaction.CancellationToken);

      mimeDetectionReport.Data.Mime = definition?.File.MimeType;
      await mimeDetectionReport.Save(transaction);

      return definition;
    }
    else
    {
      return context
        .Definitions.Where((item) => item.File.MimeType == mimeDetectionReport.Data.Mime)
        .FirstOrDefault();
    }
  }

  protected override Task OnStop(MimeDetectorContext context, ExceptionDispatchInfo? exception)
  {
    return base.OnStop(context, exception);
  }
}
