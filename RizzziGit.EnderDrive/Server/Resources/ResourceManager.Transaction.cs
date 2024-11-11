using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace RizzziGit.EnderDrive.Server.Resources;

using System.Linq;
using Commons.Collections;
using Commons.Logging;
using Commons.Services;
using RizzziGit.Commons.Utilities;
using Utilities;

public sealed partial class ResourceManager
{
    private ulong TransactionId = 0;

    public Task Transact(
        ResourceTransaction? existing,
        ResourceTransactionCallback callback,
        CancellationToken cancellationToken = default
    ) =>
        existing != null
            ? callback(existing with { CancellationToken = cancellationToken })
            : Transact(callback, cancellationToken);

    public Task<T> Transact<T>(
        ResourceTransaction? exception,
        ResourceTransactionCallback<T> callback,
        CancellationToken cancellationToken = default
    ) =>
        exception != null
            ? callback(exception with { CancellationToken = cancellationToken })
            : Transact<T>(callback, cancellationToken);

    public async Task Transact(
        ResourceTransactionCallback callback,
        CancellationToken cancellationToken = default
    )
    {
        CancellationToken serviceCancellationToken = GetCancellationToken();

        async Task transactInner(ulong transactionId, Logger logger)
        {
            using CancellationTokenSource linkedCancellationTokenSource = cancellationToken.Link(
                serviceCancellationToken
            );

            using IClientSessionHandle session = await Client.StartSessionAsync(
                null,
                linkedCancellationTokenSource.Token
            );

            session.StartTransaction();

            try
            {
                await callback(
                    new()
                    {
                        TransactionId = transactionId,
                        Session = session,
                        CancellationToken = linkedCancellationTokenSource.Token,
                        Logger = logger,
                    }
                );

                await session.CommitTransactionAsync(linkedCancellationTokenSource.Token);
            }
            catch (Exception exception)
            {
                try
                {
                    await session.AbortTransactionAsync(linkedCancellationTokenSource.Token);
                }
                catch (Exception abortException)
                {
                    throw new AggregateException(exception, abortException);
                }
            }
        }

        ulong transactionId = ++TransactionId;
        Logger transactionLogger = new($"Transaction #{transactionId}");
        try
        {
            ((IService)this).Logger.Subscribe(transactionLogger);

            try
            {
                transactionLogger.Info("Started");

                await transactInner(transactionId, transactionLogger);

                transactionLogger.Info("Completed");
            }
            catch (Exception exception)
            {
                transactionLogger.Info("Errored");
                transactionLogger.Error($"{exception}");

                throw;
            }
        }
        finally
        {
            ((IService)this).Logger.Unsubscribe(transactionLogger);
        }
    }

    public Task<T> Transact<T>(
        ResourceTransactionCallback<T> callback,
        CancellationToken cancellationToken = default
    )
    {
        TaskCompletionSource<T> source = new();

        _ = Transact(
            async (parameters) =>
            {
                try
                {
                    source.SetResult(await callback(parameters));
                }
                catch (Exception exception)
                {
                    source.SetException(exception);
                }
            },
            cancellationToken
        );

        return source.Task;
    }

    public async IAsyncEnumerable<T> Transact<T>(
        ResourceTransactionCallbackEnumerable<T> callback,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        WaitQueue<TaskCompletionSource<StrongBox<T>?>> queue = new();

        _ = Transact(
            async (parameters) =>
            {
                try
                {
                    await foreach (T item in callback(parameters))
                        (await queue.Dequeue(cancellationToken)).SetResult(new(item));

                    (await queue.Dequeue(cancellationToken)).SetResult(null);
                }
                catch (Exception exception)
                {
                    (await queue.Dequeue(cancellationToken)).SetException(exception);
                }
            },
            cancellationToken
        );

        while (true)
        {
            TaskCompletionSource<StrongBox<T>?> source = new();
            await queue.Enqueue(source, CancellationToken.None);

            StrongBox<T>? result = await source.Task;
            if (result == null)
            {
                yield break;
            }

            yield return result.Value!;
        }
    }
}

public sealed record class ResourceTransaction 
{
    public static implicit operator CancellationToken(ResourceTransaction parameters) =>
        parameters.CancellationToken;

    public required ulong TransactionId;
    public required Logger Logger;

    public required IClientSessionHandle Session;
    public required CancellationToken CancellationToken;
}

public delegate Task ResourceTransactionCallback(ResourceTransaction parameters);
public delegate Task<T> ResourceTransactionCallback<T>(ResourceTransaction parameters);
public delegate IAsyncEnumerable<T> ResourceTransactionCallbackEnumerable<T>(
    ResourceTransaction parameters
);
