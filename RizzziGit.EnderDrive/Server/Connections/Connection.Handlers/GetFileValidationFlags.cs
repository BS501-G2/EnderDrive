using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using RizzziGit.EnderDrive.Server.Resources;

namespace RizzziGit.EnderDrive.Server.Connections;

public sealed partial class Connection
{
  private sealed record class GetFileNameValidationFlagsRequest
  {
    [BsonElement("name")]
    public required string Name;

    [BsonElement("parentId")]
    public required ObjectId ParentId;
  }

  private sealed record class GetFileNameValidationFlagsResponse
  {
    [BsonElement("flags")]
    public FileNameValidationFlags Flags;
  }

  private TransactedRequestHandler<
    GetFileNameValidationFlagsRequest,
    GetFileNameValidationFlagsResponse
  > GetFileNameValidationFlags =>
    async (transaction, request) =>
    {
      Resource<File> file = await Internal_EnsureFirst(
        transaction,
        Resources.Query<File>(
          transaction,
          (query) => query.Where((file) => file.Id == request.ParentId)
        )
      );

      return new() { Flags = await Resources.ValidateFileName(transaction, request.Name, file) };
    };
}
