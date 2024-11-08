using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace RizzziGit.EnderDrive.Server.Services;

using System;
using Commons.Collections;
using Commons.Services;
using Core;
using RizzziGit.EnderDrive.Utilities;

public sealed class KeyGeneratorParams
{
    public required WaitQueue<RSA> AsymmetricKeys;
    public required WaitQueue<Aes> SymmetricKeys;
}

public sealed class KeyManager(Server server) : Service<KeyGeneratorParams>("Key Manager", server)
{
    private static async Task RunAsymmetricKeyGenerator(
        WaitQueue<RSA> queue,
        CancellationToken cancellationToken
    )
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            RSA key = RSA.Create(2048);

            await queue.Enqueue(key, cancellationToken);
        }
    }

    private static async Task RunSymmetricKeyGenerator(
        WaitQueue<Aes> queue,
        CancellationToken cancellationToken
    )
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Aes key = Aes.Create();
            key.KeySize = 256;

            key.GenerateKey();
            key.GenerateIV();

            await queue.Enqueue(key, cancellationToken);
        }
    }

    protected override Task<KeyGeneratorParams> OnStart(
        CancellationToken startupCancellationToken,
        CancellationToken serviceCancellationToken
    )
    {
        WaitQueue<RSA> asymmetricKeys = new(1000);
        WaitQueue<Aes> symmetricKeys = new(1000);

        return Task.FromResult<KeyGeneratorParams>(
            new() { AsymmetricKeys = asymmetricKeys, SymmetricKeys = symmetricKeys }
        );
    }

    protected override async Task OnRun(
        KeyGeneratorParams data,
        CancellationToken cancellationToken
    )
    {
        KeyGeneratorParams context = GetContext();
        Task[] tasks =
        [
            Task.Run(
                () => RunAsymmetricKeyGenerator(context.AsymmetricKeys, cancellationToken),
                CancellationToken.None
            ),
            Task.Run(
                () => RunSymmetricKeyGenerator(context.SymmetricKeys, cancellationToken),
                CancellationToken.None
            ),
        ];

        await Task.WhenAny(tasks);
        WaitTasksBeforeStopping.AddRange(tasks);
    }

    public Task<RSA> GenerateAsymmetricKey(CancellationToken cancellationToken = default) =>
        GetContext().AsymmetricKeys.Dequeue(cancellationToken);

    public static byte[] SerializeAsymmetricKey(RSA key, bool includePrivate)
    {
        RSAParameters keyParameters = key.ExportParameters(includePrivate);

        using MemoryStream stream = new();
        using BsonWriter bsonWriter = new BsonBinaryWriter(stream);

        BsonSerializer.Serialize(bsonWriter, keyParameters);

        return stream.ToArray();
    }

    public static RSA DeserializeAsymmetricKey(byte[] bytes)
    {
        using MemoryStream stream = new(bytes);
        using BsonReader bsonReader = new BsonBinaryReader(stream);

        RSAParameters keyParameters = BsonSerializer.Deserialize<RSAParameters>(bsonReader);
        return RSA.Create(keyParameters);
    }

    public static byte[] Encrypt(RSA key, byte[] input) =>
        key.Encrypt(input, RSAEncryptionPadding.OaepSHA512);

    public static byte[] Decrypt(RSA key, byte[] input) =>
        key.Decrypt(input, RSAEncryptionPadding.OaepSHA512);

    public static byte[] Encrypt(byte[] aesKey, byte[] input)
    {
        byte[] aesIv = RandomNumberGenerator.GetBytes(16);

        using Aes aes = Aes.Create();
        using ICryptoTransform encryptor = aes.CreateEncryptor(aesKey, aesIv);

        byte[] encrypted = encryptor.TransformFinalBlock(input, 0, input.Length);
        byte[] output = new byte[encrypted.Length + 16];

        Buffer.BlockCopy(aesIv, 0, output, 0, aesIv.Length);
        Buffer.BlockCopy(encrypted, 0, output, aesIv.Length, encrypted.Length);

        return output;
    }

    public static byte[] Decrypt(byte[] aesKey, byte[] input)
    {
        using Aes aes = Aes.Create();
        using ICryptoTransform decryptor = aes.CreateDecryptor(aesKey, input[..16]);

        return decryptor.TransformFinalBlock(input, 16, input.Length - 16);
    }
}
