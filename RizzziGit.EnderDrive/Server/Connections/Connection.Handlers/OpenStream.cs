using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

using System.Linq;
using System.Threading.Tasks;
using Resources;

public sealed partial class Connection
{
  private sealed record class OpenStreamRequest
  {
    [BsonElement("fileId")]
    public required ObjectId FileId;

    [BsonElement("fileContentId")]
    public required ObjectId FileContentId;

    [BsonElement("fileSnapshotId")]
    public required ObjectId FileSnapshotId;
  }

  private sealed record class OpenStreamResponse
  {
    [BsonElement("streamId")]
    public required ObjectId StreamId;
  }

  private AuthenticatedRequestHandler<OpenStreamRequest, OpenStreamResponse> OpenStream =>
    async (transaction, request, userAuthentication, me, _) =>
    {
      ConnectionContext context = GetContext();

      Resource<File> file = await Internal_EnsureFirst(
        transaction,
        Resources.Query<File>(
          transaction,
          (query) => query.Where((item) => item.Id == request.FileId)
        )
      );

      FileAccessResult fileAccessResult = await Internal_UnlockFile(
        transaction,
        file,
        me,
        userAuthentication
      );

      Resource<FileContent> fileContent = await Resources.GetMainFileContent(transaction, file);

      Resource<FileSnapshot> fileSnapshot =
        await Resources
          .Query<FileSnapshot>(
            transaction,
            (query) =>
              query
                .Where((item) => item.Id == request.FileSnapshotId)
                .OrderByDescending((item) => item.CreateTime)
          )
          .FirstOrDefaultAsync(transaction.CancellationToken)
        ?? await Resources.CreateFileSnapshot(
          transaction,
          fileAccessResult.UnlockedFile,
          fileContent,
          userAuthentication,
          null
        );

      TaskCompletionSource<ObjectId> source = new();

      RunStream(
        fileAccessResult.UnlockedFile,
        fileContent,
        fileSnapshot,
        userAuthentication,
        source
      );

      return new() { StreamId = await source.Task };
    };
}
