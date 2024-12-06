using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace RizzziGit.EnderDrive.Server.Resources;

using Commons.Collections;
using Commons.Services;
using Commons.Tasks;
using Core;
using Services;

public abstract record ResourceData
{
  [BsonId]
  public ObjectId Id = ObjectId.Empty;

  internal object ToJSON()
  {
    throw new NotImplementedException();
  }
}

public sealed class ResourceManagerContext
{
  public required ILoggerFactory LoggerFactory;
  public required IMongoClient Client;
  public required RandomNumberGenerator RandomNumberGenerator;

  public required WeakDictionary<ResourceKey, object> Resources;
  public required ConcurrentDictionary<
    ObjectId,
    ResourceManager.FileResourceStream
  > ActiveFileStreams;

  public required TaskQueue TransactionQueue;
}

public sealed partial class ResourceManager(EnderDriveServer server)
  : Service<ResourceManagerContext>("Resource Manager", server)
{
  private EnderDriveServer Server => server;
  private IMongoClient Client => GetContext().Client;
  private IMongoDatabase Database => Client.GetDatabase("EnderDrive");

  private IMongoCollection<D> GetCollection<D>()
    where D : ResourceData => Database.GetCollection<D>(typeof(D).Name);

  private KeyManager KeyManager => server.KeyManager;
  private AdminManager AdminManager => server.AdminManager;

  protected override async Task<ResourceManagerContext> OnStart(
    CancellationToken startupCancellationToken,
    CancellationToken serviceCancellationToken
  )
  {
    ILoggerFactory loggerFactory = LoggerFactory.Create(
      (options) => options.AddProvider(new LoggerProvider((category) => new LoggerInstance(this)))
    );

    MongoClient client =
      new(
        new MongoClientSettings()
        {
          Server = new MongoServerAddress("127.0.0.1"),
          LoggingSettings = new(loggerFactory),
        }
      );

    RandomNumberGenerator randomNumberGenerator = RandomNumberGenerator.Create();

    if (Environment.GetCommandLineArgs().Contains("--delete-db"))
    {
      await client.DropDatabaseAsync("EnderDrive", startupCancellationToken);
    }

    NotificationData.BindPolymorphicTypes();

    return new()
    {
      LoggerFactory = loggerFactory,
      Client = client,
      RandomNumberGenerator = randomNumberGenerator,
      Resources = [],
      TransactionQueue = new(),
      ActiveFileStreams = new()
    };
  }

  protected override async Task OnRun(
    ResourceManagerContext context,
    CancellationToken serviceCancellationToken
  )
  {
    await context.TransactionQueue.Start(serviceCancellationToken);
  }

  protected override Task OnStop(ResourceManagerContext context, ExceptionDispatchInfo? exception)
  {
    return base.OnStop(context, exception);
  }
}
