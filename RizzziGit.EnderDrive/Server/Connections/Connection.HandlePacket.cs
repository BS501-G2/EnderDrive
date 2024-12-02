using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using RizzziGit.EnderDrive.Server.Resources;

namespace RizzziGit.EnderDrive.Server.Connections;

public sealed partial class Connection
{
  private async Task HandlePacket(
    ConnectionContext context,
    ConnectionPacket packet,
    CancellationToken cancellationToken
  )
  {
    switch (packet)
    {
      case ConnectionPacket.Request request:
      {
        if (!context.Handlers.TryGetValue(request.Name, out RawRequestHandler? handler))
        {
          await Send(
            new ConnectionPacket.Error()
            {
              Id = request.Id,
              Message = $"No request handler found for request {request.Name}",
              Stack = ExceptionDispatchInfo.SetCurrentStackTrace(new Exception()).StackTrace
            },
            cancellationToken
          );

          break;
        }

        byte[] data;

        try
        {
          data = await handler(request.Data, cancellationToken);
        }
        catch (Exception exception)
        {
          Error(exception);
          await Send(
            new ConnectionPacket.Error()
            {
              Id = request.Id,
              Message = exception.Message,
              Stack = exception.StackTrace
            },
            cancellationToken
          );
          break;
        }

        await Send(
          new ConnectionPacket.Response() { Id = request.Id, Data = data },
          cancellationToken
        );

        break;
      }

      case ConnectionPacket.Response response:
      {
        if (
          !context.PendingRequests.TryRemove(response.Id, out TaskCompletionSource<byte[]>? result)
        )
        {
          break;
        }

        result.SetResult(response.Data);
        break;
      }

      case ConnectionPacket.Error error:
      {
        if (!context.PendingRequests.TryRemove(error.Id, out TaskCompletionSource<byte[]>? result))
        {
          break;
        }

        result.SetException(new ConnectionResponseException(error.Message));

        break;
      }
    }
  }
}

public class ConnectionResponseException(string message) : Exception(message);

public class InsufficientRoleException() : ConnectionResponseException("Insufficient roles")
{
  [BsonElement("includeRoles")]
  public required UserRole[]? IncludeRoles;

  [BsonElement("excludeRoles")]
  public required UserRole[]? ExcludeRoles;
}

public class NotFoundException() : ConnectionResponseException("Resource not found.")
{
  [BsonElement("resourceName")]
  public required string ResourceName;
}

public class AdminRequired()
  : ConnectionResponseException("Admin access required to access this endpoint.") { }

public class FileAccessForbiddenException()
  : ConnectionResponseException("No access for this file.")
{
  [BsonElement("fileId")]
  public required ObjectId FileId;
}

public class FileNameConflictException()
  : ConnectionResponseException("File name already exists or is invalid.")
{
  [BsonElement("name")]
  public required string Name;

  [BsonElement("parentFileId")]
  public required ObjectId? ParentFileId;
}
