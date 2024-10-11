using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace RizzziGit.EnderDrive.Server.Resources;

using System.Collections.Concurrent;
using RizzziGit.Commons.Memory;
using Utilities;

public sealed partial class ResourceManager
{
    private sealed record FileStreamKey(
        ObjectId FileId,
        ObjectId FileContentId,
        ObjectId FileSnapshotId
    );

    private readonly ConcurrentDictionary<FileStreamKey, FileStream> FileStreams = new();

    public async Task<Stream> CreateReadStream(
        ResourceTransaction transaction,
        UnlockedFile file,
        FileContent content,
        FileSnapshot snapshot
    )
    {
        FileStreamKey key = new(file.Id, content.Id, snapshot.Id);
        if (FileStreams.TryGetValue(key, out FileStream? stream))
        {
            throw new InvalidOperationException("Existing file stream is active.");
        }
        else if (
            !FileStreams.TryAdd(
                key,
                stream = new FileStream(
                    this,
                    transaction,
                    file,
                    content,
                    snapshot,
                    await GetFileSize(transaction, snapshot),
                    null
                )
            )
        )
        {
            throw new InvalidOperationException("Failed to create new file stream.");
        }

        return stream;
    }

    public async Task<Stream> CreateWriteStream(
        ResourceTransaction transaction,
        UnlockedFile file,
        FileContent content,
        FileSnapshot snapshot,
        UnlockedUserAuthentication userAuthentication,
        bool createNewSnapshot
    )
    {
        FileStreamKey key = new(file.Id, content.Id, snapshot.Id);
        if (FileStreams.TryGetValue(key, out FileStream? stream))
        {
            throw new InvalidOperationException("Existing file stream is active.");
        }
        else if (
            !FileStreams.TryAdd(
                key,
                stream = new FileStream(
                    this,
                    transaction,
                    file,
                    content,
                    createNewSnapshot
                        ? await CreateFileSnapshot(
                            transaction,
                            file,
                            content,
                            userAuthentication,
                            snapshot
                        )
                        : snapshot,
                    await GetFileSize(transaction, snapshot),
                    userAuthentication
                )
            )
        )
        {
            throw new InvalidOperationException("Failed to create new file stream.");
        }

        return stream;
    }

    private sealed class FileStream(
        ResourceManager manager,
        ResourceTransaction? transaction,
        UnlockedFile file,
        FileContent content,
        FileSnapshot snapshot,
        long size,
        UnlockedUserAuthentication? userAuthentication
    ) : Stream
    {
        public readonly UnlockedFile File = file;
        public readonly FileContent FileContent = content;
        public readonly FileSnapshot FileSnapshot = snapshot;

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => userAuthentication != null;

        public override long Length => size;

        private long size = size;
        private long position = 0;

        public override long Position
        {
            get => position;
            set
            {
                if (value >= 0 && value <= size)
                {
                    position = value;
                }
                else
                    throw new ArgumentOutOfRangeException(
                        nameof(value),
                        value,
                        $"Must must range from 0 to {size}"
                    );
            }
        }

        public override void Flush() { }

        public override async ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default
        ) =>
            await Task.Run(
                async () =>
                {
                    var cancellationTokenSource = cancellationToken.Link(
                        transaction?.CancellationToken
                    );

                    CompositeBuffer fileRead = await manager.Transact(
                        transaction,
                        (transaction) =>
                            manager
                                .ReadFile(
                                    transaction,
                                    File,
                                    FileContent,
                                    FileSnapshot,
                                    Position,
                                    long.Min(buffer.Span.Length, Length - Position)
                                )
                                .WaitAsync(cancellationTokenSource.Token),
                        cancellationTokenSource.Token
                    );

                    fileRead.ToByteArray().CopyTo(buffer.Span);
                    position += fileRead.Length;
                    return (int)fileRead.Length;
                },
                default
            );

        public override async ValueTask WriteAsync(
            ReadOnlyMemory<byte> buffer,
            CancellationToken cancellationToken = default
        ) =>
            await Task.Run(
                async () =>
                {
                    if (userAuthentication is null)
                    {
                        throw new InvalidOperationException("Not authenticated.");
                    }

                    var cancellationTokenSource = cancellationToken.Link(
                        transaction?.CancellationToken
                    );

                    await manager.Transact(
                        transaction,
                        (transaction) =>
                            manager
                                .WriteFile(
                                    transaction,
                                    File,
                                    FileContent,
                                    FileSnapshot,
                                    userAuthentication,
                                    position,
                                    buffer.ToArray()
                                )
                                .WaitAsync(cancellationTokenSource.Token),
                        cancellationTokenSource.Token
                    );

                    position += buffer.Length;
                    size = long.Max(position, size);
                },
                default
            );

        public override void SetLength(long value) { }

        public override long Seek(long offset, SeekOrigin origin) =>
            origin switch
            {
                SeekOrigin.Begin => Position = offset,
                SeekOrigin.Current => Position += offset,
                SeekOrigin.End => Position = Length - offset,
                _ => throw new InvalidOperationException("Invalid offset."),
            };

        public override void Write(byte[] buffer, int offset, int count) =>
            WriteAsync(buffer, 0, 0, CancellationToken.None).GetAwaiter().GetResult();

        public override async Task WriteAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken
        ) => await WriteAsync(buffer.AsMemory(offset, count), cancellationToken);

        public override int Read(byte[] buffer, int offset, int count) =>
            ReadAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();

        public override async Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken
        ) => await ReadAsync(buffer.AsMemory(offset, count), cancellationToken);
    }
}
