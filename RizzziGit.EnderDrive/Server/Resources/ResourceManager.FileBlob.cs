using System;
using System.IO;
using System.Security.Cryptography;
using MongoDB.Bson;

namespace RizzziGit.EnderDrive.Server.Resources;

using System.Linq;
using Services;

public sealed record FileBlob
{
  public required ObjectId Id;

  public required long Start;
  public required long End;

  public long Size => End - Start;

  public required byte[] Checksum;
}

public sealed partial class ResourceManager
{
  private static void SeekBlobStream(FileStream stream, long position)
  {
    if (position != stream.Seek(position, SeekOrigin.Begin))
    {
      throw new InvalidDataException("Failed to properly seek to the required position.");
    }
  }

  private static byte[] ReadBlobStream(FileStream stream, long length)
  {
    byte[] bytes = new byte[length];
    if (length != stream.Read(bytes, 0, bytes.Length))
    {
      throw new InvalidDataException("Failed to read the exact required length of data.");
    }

    return bytes;
  }

  private static void WriteBlobStream(FileStream stream, byte[] data)
  {
    try
    {
      stream.Write(data);
    }
    catch (Exception exception)
    {
      throw new InvalidOperationException("Failed to write blob data.", exception);
    }
  }

  private static byte[] DecryptBlobData(byte[] fileAesKey, byte[] encryptedBytes)
  {
    byte[] decrypted;

    try
    {
      decrypted = KeyManager.Decrypt(fileAesKey, encryptedBytes);
    }
    catch (Exception exception)
    {
      throw new InvalidDataException("Failed to decrypt encrypted bytes.", exception);
    }

    return decrypted;
  }

  private static byte[] EncryptBlobData(byte[] fileAesKey, byte[] bytes)
  {
    byte[] encrypted;

    try
    {
      encrypted = KeyManager.Encrypt(fileAesKey, bytes);
    }
    catch (Exception exception)
    {
      throw new InvalidOperationException("failed to encrypt bytes.", exception);
    }

    return encrypted;
  }

  private static void ValidateChecksum(byte[] checksum, byte[] bytes)
  {
    if (!SHA256.HashData(bytes).SequenceEqual(checksum))
    {
      throw new InvalidDataException("Decrypted data did not pass checksum");
    }
  }

  private static byte[] CalculateChecksum(byte[] bytes)
  {
    return SHA256.HashData(bytes);
  }

  private FileBlob WriteToBlob(byte[] fileAesKey, byte[] data)
  {
    FileStream stream = GetContext().BlobStream;

    byte[] encrypted = EncryptBlobData(fileAesKey, data);
    byte[] checksum = CalculateChecksum(data);

    FileBlob fileBlob;

    lock (stream)
    {
      fileBlob = new()
      {
        Id = ObjectId.GenerateNewId(),

        Start = stream.Length,
        End = stream.Length + encrypted.Length,

        Checksum = checksum
      };

      SeekBlobStream(stream, fileBlob.Start);
      WriteBlobStream(stream, encrypted);
    }

    return fileBlob;
  }

  private byte[] ReadFromBlob(FileBlob fileBlob, byte[] fileAesKey)
  {
    FileStream stream = GetContext().BlobStream;

    byte[] decrypted;

    lock (stream)
    {
      SeekBlobStream(stream, fileBlob.Start);
      byte[] encrypted = ReadBlobStream(stream, fileBlob.Size);
      decrypted = DecryptBlobData(fileAesKey, encrypted);
      ValidateChecksum(fileBlob.Checksum, decrypted);
    }

    return decrypted;
  }
}
