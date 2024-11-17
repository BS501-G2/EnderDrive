using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Newtonsoft.Json;

namespace RizzziGit.EnderDrive.Server.Resources;

using Commons.Services;
using Core;
using Services;

public abstract record class ResourceData
{
  [BsonId]
  [JsonProperty(
    "id"
  )]
  public required ObjectId Id;

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
}

public sealed record FileStreamKey(
  ObjectId FileId,
  ObjectId FileContentId,
  ObjectId FileSnapshotId
);

public sealed class Resource<D>(
  Func<D> getData,
  Func<
    ResourceTransaction,
    Task
  > update,
  Func<
    ResourceTransaction,
    Task
  > reset
)
  where D : ResourceData
{
  public static implicit operator D(
    Resource<D> resource
  ) =>
    resource.Data;

  public static explicit operator Resource<D>(
    Resource<ResourceData> v
  )
  {
    throw new NotImplementedException();
  }

  public D Data =>
    getData();

  public Task Save(
    ResourceTransaction transaction
  ) =>
    update(
      transaction
    );

  public Task Reset(
    ResourceTransaction transaction
  ) =>
    reset(
      transaction
    );
}

public sealed partial class ResourceManager(
  Server server
)
  : Service<ResourceManagerContext>(
    "Resource Manager",
    server
  )
{
  private Server Server =>
    server;
  private IMongoClient Client =>
    GetContext().Client;
  private IMongoDatabase Database =>
    Client.GetDatabase(
      "EnderDrive"
    );

  private IMongoCollection<D> GetCollection<D>()
    where D : ResourceData =>
    Database.GetCollection<D>(
      typeof(D).Name
    );

  private KeyManager KeyManager =>
    server.KeyManager;
  private AdminManager AdminManager =>
    server.AdminManager;

  protected override async Task<ResourceManagerContext> OnStart(
    CancellationToken startupCancellationToken,
    CancellationToken serviceCancellationToken
  )
  {
    ILoggerFactory loggerFactory =
      LoggerFactory.Create(
        (
          options
        ) =>
          options.AddProvider(
            new LoggerProvider(
              (
                category
              ) =>
                new LoggerInstance(
                  this
                )
            )
          )
      );

    MongoClient client =
      new(
        new MongoClientSettings()
        {
          Server =
            new MongoServerAddress(
              "127.0.0.1"
            ),
          LoggingSettings =
            new(
              loggerFactory
            ),
        }
      );

    RandomNumberGenerator randomNumberGenerator =
      RandomNumberGenerator.Create();

    if (
      Environment
        .GetCommandLineArgs()
        .Contains(
          "--delete-db"
        )
    )
    {
      await client.DropDatabaseAsync(
        "EnderDrive",
        startupCancellationToken
      );
    }

    return new()
    {
      LoggerFactory =
        loggerFactory,
      Client =
        client,
      RandomNumberGenerator =
        randomNumberGenerator,
    };
  }
}
