using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace RizzziGit.EnderDrive.Server.Resources;

using Commons.Collections;
using Utilities;

public sealed record class ResourceKey(Type Type, ObjectId ObjectId);

public record class Resource<T>
  where T : ResourceData
{
  public delegate ValueTask OnSave(
    ResourceTransaction transaction,
    Resource<T> data
  );
  public delegate ValueTask OnDelete(
    ResourceTransaction transaction,
    Resource<T> data
  );

  public Resource(OnSave save)
  {
    this.save = save;
  }

  public ObjectId Id
  {
    get => Data.Id;
    set => Data.Id = value;
  }

  public required T Data;

  private readonly OnSave save;

  public ValueTask Save(ResourceTransaction transaction) =>
    save(transaction, this);

  public ValueTask Update(Func<T, T> callback, ResourceTransaction transaction)
  {
    Data = callback(Data);
    return Save(transaction);
  }

  public string ToJson() => Data.ToJson();
}

public static class ResourceExtensions
{
  public static IEnumerable<string> ToJson<T>(
    this IEnumerable<Resource<T>> resources
  )
    where T : ResourceData => resources.Select(r => r.ToJson());

  public static IEnumerable<string> ToJson<T>(
    this IEnumerable<UnlockedResource<T>> resources
  )
    where T : ResourceData => resources.Select(r => r.ToJson());
}

public sealed partial class ResourceManager
{
  private async ValueTask Save<T>(
    ResourceTransaction transaction,
    Resource<T> document
  )
    where T : ResourceData
  {
    IMongoCollection<T> collection = GetCollection<T>();

    T? oldData =
      document.Id == ObjectId.Empty
        ? null
        : await collection
          .AsQueryable()
          .Where((item) => item.Id == document.Id)
          .FirstOrDefaultAsync(
            cancellationToken: transaction.CancellationToken
          );

    if (oldData == null)
    {
      ObjectId oldId = document.Id;

      document.Id = ObjectId.GenerateNewId();

      transaction.RegisterOnFailure(async () =>
      {
        document.Id = oldId;
      });

      await collection.InsertOneAsync(
        document: document.Data,
        cancellationToken: transaction.CancellationToken
      );

      return;
    }

    transaction.RegisterOnFailure(async () =>
    {
      document.Data = oldData;
    });

    await collection.ReplaceOneAsync(
      filter: (item) => item.Id == document.Id,
      replacement: document.Data,
      options: new ReplaceOptions() { IsUpsert = true },
      cancellationToken: transaction.CancellationToken
    );
  }

  private async ValueTask Delete<T>(
    ResourceTransaction transaction,
    Resource<T> document
  )
    where T : ResourceData
  {
    ResourceManagerContext context = GetContext();

    IMongoCollection<T> collection = GetCollection<T>();

    if (document.Id == ObjectId.Empty)
    {
      return;
    }

    lock (context.Resources)
    {
      context.Resources.Remove(new ResourceKey(typeof(T), document.Id));

      transaction.RegisterOnFailure(async () =>
      {
        lock (context.Resources)
        {
          context.Resources.Add(
            new ResourceKey(typeof(T), document.Id),
            document
          );
        }
      });
    }

    await collection.DeleteOneAsync(
      filter: (item) => item.Id == document.Id,
      cancellationToken: transaction.CancellationToken
    );
  }

  public IAsyncEnumerable<Resource<T>> Query<T>(
    ResourceTransaction transaction,
    Func<IQueryable<T>, IQueryable<T>> filter
  )
    where T : ResourceData =>
    filter(GetCollection<T>().AsQueryable())
      .ToAsyncEnumerable()
      .Select((item) => ToResource(transaction, item));

  private Resource<T> ToResource<T>(ResourceTransaction transaction, T data)
    where T : ResourceData
  {
    ResourceManagerContext context = GetContext();

    lock (context.Resources)
    {
      if (
        context.Resources.TryGetValue(new(typeof(T), data.Id), out object? raw)
      )
      {
        if (raw is Resource<T> resourceItem)
        {
          resourceItem.Data = data;
          return resourceItem;
        }

        context.Resources.Remove(new ResourceKey(typeof(T), data.Id));

        transaction.RegisterOnFailure(async () =>
        {
          lock (context.Resources)
          {
            context.Resources.Add(
              new ResourceKey(typeof(T), data.Id),
              (Resource<T>)raw!
            );
          }
        });
      }

      Resource<T> resource = new(Save) { Data = data };
      context.Resources.Add(new(typeof(T), data.Id), resource);

      transaction.RegisterOnFailure(async () =>
      {
        lock (context.Resources)
        {
          context.Resources.Remove(new ResourceKey(typeof(T), data.Id));
        }
      });

      return resource;
    }
  }
}
