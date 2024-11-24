using System;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace RizzziGit.EnderDrive.Server.Connections;

using Commons.Collections;
using Commons.Memory;
using Commons.Utilities;
using Resources;

public sealed partial class Connection
{
  private async void RunStream(
    UnlockedFile file,
    Resource<FileContent> fileContent,
    Resource<FileSnapshot> fileSnapshot,
    Resource<User> user,
    UnlockedUserAuthentication userAuthentication,
    TaskCompletionSource<ObjectId> source
  ) =>
    await Resources.Transact(
      async (transaction) =>
      {
        bool written = false;
        ConnectionContext context = GetContext();

        ObjectId objectId = ObjectId.GenerateNewId();
        WaitQueue<ConnectionByteStream.Feed> queue = new(0);
        ConnectionByteStream stream = new(objectId, queue);

        if (!context.FileStreams.TryAdd(objectId, stream))
        {
          source.SetException(new InvalidOperationException("Failed to create a file stream."));

          return;
        }

        source.SetResult(objectId);

        long offset = 0;

        async Task<bool> handleFeed(
          ConnectionByteStream.Feed feed,
          CancellationToken cancellationToken
        )
        {
          switch (feed)
          {
            case ConnectionByteStream.Feed.Truncate(
              TaskCompletionSource<ObjectId?> source,
              long length,
              _
            ):
            {
              try
              {
                ObjectId? newId = null;
                if (!written)
                {
                  await Resources.CreateFileLog(transaction, file.File, user, FileLogType.Update);
                  fileSnapshot = await Resources.CreateFileSnapshot(
                    transaction,
                    file,
                    fileContent,
                    userAuthentication,
                    fileSnapshot
                  );

                  written = true;

                  newId = fileSnapshot.Id;
                }

                await fileSnapshot.Update(
                  (data) =>
                  {
                    data.Size = length;
                  },
                  transaction
                );

                offset = long.Min(offset, length);

                source.SetResult(newId);
              }
              catch (Exception exception)
              {
                source.SetException(exception);
              }

              break;
            }

            case ConnectionByteStream.Feed.Write(
              TaskCompletionSource<ObjectId?> source,
              CompositeBuffer bytes,
              _
            ):
            {
              try
              {
                ObjectId? newId = null;
                if (!written)
                {
                  fileSnapshot = await Resources.CreateFileSnapshot(
                    transaction,
                    file,
                    fileContent,
                    userAuthentication,
                    fileSnapshot
                  );

                  written = true;

                  newId = fileSnapshot.Id;
                }

                await Resources.WriteFile(
                  transaction,
                  file,
                  fileContent,
                  fileSnapshot,
                  userAuthentication,
                  offset,
                  bytes
                );

                offset += bytes.Length;

                source.SetResult(newId);
              }
              catch (Exception exception)
              {
                source.SetException(exception);
              }

              break;
            }

            case ConnectionByteStream.Feed.Read(
              TaskCompletionSource<CompositeBuffer> source,
              long count,
              _
            ):
            {
              try
              {
                CompositeBuffer bytes = await Resources.ReadFile(
                  transaction,
                  file,
                  fileContent,
                  fileSnapshot,
                  offset,
                  count
                );

                offset += bytes.Length;
                source.SetResult(bytes);
              }
              catch (Exception exception)
              {
                source.SetException(exception);
              }

              break;
            }

            case ConnectionByteStream.Feed.SetPosition(
              TaskCompletionSource source,
              long newOffset,
              _
            ):
            {
              try
              {
                if (newOffset > fileSnapshot.Data.Size)
                {
                  source.SetException(new ArgumentOutOfRangeException());
                }
                else
                {
                  offset = newOffset;
                  source.SetResult();
                }
              }
              catch (Exception exception)
              {
                source.SetException(exception);
              }

              break;
            }

            case ConnectionByteStream.Feed.GetPosition(TaskCompletionSource<long> source, _):
            {
              try
              {
                source.SetResult(offset);
              }
              catch (Exception exception)
              {
                source.SetException(exception);
              }

              break;
            }

            case ConnectionByteStream.Feed.GetLength(TaskCompletionSource<long> source, _):
            {
              try
              {
                source.SetResult(fileSnapshot.Data.Size);
              }
              catch (Exception exception)
              {
                source.SetException(exception);
              }

              break;
            }

            case ConnectionByteStream.Feed.Close(TaskCompletionSource source, _):
            {
              source.SetResult();
              return false;
            }
          }

          return true;
        }

        try
        {
          await foreach (
            ConnectionByteStream.Feed feed in queue.WithCancellation(transaction.CancellationToken)
          )
          {
            using CancellationTokenSource linkedCancellationTokenSource =
              feed.CancellationToken.Link(transaction.CancellationToken);

            if (!await handleFeed(feed, linkedCancellationTokenSource.Token))
            {
              break;
            }
          }
        }
        catch (Exception exception)
        {
          await Resources.Delete(transaction, file.File);

          Error(exception);
          queue.Dispose(exception);
        }
        finally
        {
          context.FileStreams.TryRemove(objectId, out _);
        }
      },
      CancellationToken.None
    );
}
