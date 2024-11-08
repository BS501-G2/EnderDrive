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

namespace RizzziGit.EnderDrive.Server.Services;

using System.Linq;
using Commons.Collections;
using Commons.Services;
using Commons.Utilities;
using Core;

public sealed class MimeDetectorContext
{
    public required ContentInspector ContentInspector;
    public required WaitQueue<MimeDetectorRequest> WaitQueue;
}

public sealed record MimeDetectorRequest(
    TaskCompletionSource<Definition?> Output,
    Stream Stream,
    CancellationToken CancellationToken
);

public sealed partial class MimeDetector(Server server)
    : Service<MimeDetectorContext>("Mime Detector", server)
{
    protected override Task<MimeDetectorContext> OnStart(
        CancellationToken startupCancellationToken,
        CancellationToken serviceCancellationToken
    )
    {
        Info("Building definitions...");
        ImmutableArray<Definition> definitions = new ExhaustiveBuilder()
        {
            UsageType = UsageType.PersonalNonCommercial
        }.Build();

        Info("Building content inspector...");
        ContentInspector contentInspector = new ContentInspectorBuilder()
        {
            Definitions = definitions,
            StringSegmentOptions = new()
            {
                OptimizeFor = StringSegmentResourceOptimization.HighSpeed
            }
        }.Build();

        Info("Initialization done!");
        return Task.FromResult<MimeDetectorContext>(
            new() { ContentInspector = contentInspector, WaitQueue = new() }
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
            using CancellationTokenSource linked = serviceCancellationToken.Link(cancellationToken);

            ImmutableArray<DefinitionMatch> result = context.ContentInspector.Inspect(stream);

            DefinitionMatch? match = await context
                .ContentInspector.Inspect(stream)
                .OrderByDescending((match) => match.Percentage)
                .ToAsyncEnumerable()
                .FirstOrDefaultAsync(linked.Token);

            output.SetResult(match?.Definition);
        }
    }

    public async Task<Definition?> Inspect(Stream stream, CancellationToken cancellationToken)
    {
        MimeDetectorContext context = GetContext();
        TaskCompletionSource<Definition?> output = new();

        await context.WaitQueue.Enqueue(new(output, stream, cancellationToken), cancellationToken);
        return await output.Task;
    }

    protected override Task OnStop(MimeDetectorContext context, ExceptionDispatchInfo? exception)
    {
        return base.OnStop(context, exception);
    }
}
