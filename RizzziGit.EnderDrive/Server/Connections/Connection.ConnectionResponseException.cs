using System;
using MongoDB.Bson;

namespace RizzziGit.EnderDrive.Server.Connections;

using Resources;

public enum ResponseCode : byte
{
  OK,
  Cancelled,
  InvalidParameters,
  NoHandlerFound,
  InvalidRequestCode,
  InternalError,
  AuthenticationRequired,
  AgreementRequired,
  InsufficientRole,
  ResourceNotFound,
  Forbidden,
  FileNameConflict,
}

public abstract class ConnectionException(string? message = null, Exception? inner = null)
  : Exception(message, inner);

public sealed class ConnectionResponseException(
  ResponseCode code,
  ConnectionResponseExceptionData data
) : ConnectionException($"Server returned error response: {code}")
{
  public readonly ResponseCode Code = code;
  public new readonly ConnectionResponseExceptionData Data = data;
}

public abstract record class ConnectionResponseExceptionData
{
  private ConnectionResponseExceptionData() { }

  public sealed record class AuthenticationRequired : ConnectionResponseExceptionData { }

  public sealed record class RequiredRoles : ConnectionResponseExceptionData
  {
    public required UserRole[]? IncludeRoles;
    public required UserRole[]? ExcludeRoles;
  }

  public sealed record class ResourceNotFound : ConnectionResponseExceptionData
  {
    public required string ResourceName;
  }

  public sealed record class Forbidden : ConnectionResponseExceptionData
  {
    public required ObjectId FileId;
  }

  public sealed record class FileNameConflict : ConnectionResponseExceptionData
  {
    public required string Name;
  }
}
