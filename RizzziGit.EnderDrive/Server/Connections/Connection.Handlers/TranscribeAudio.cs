using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

using Resources;

public sealed partial class Connection
{
  private sealed record class TranscribeAudioRequest : BaseFileRequest
  {
    [BsonElement("fileSnapshot")]
    public required ObjectId? FileSnapshotId;
  }

  private sealed record class TranscribeAudioResponse
  {
    [BsonElement("text")]
    public required string[] Text;

    [BsonElement("status")]
    public required AudioTranscriptionStatus Status;
  }

  private FileRequestHandler<TranscribeAudioRequest, TranscribeAudioResponse> TranscribeAudio =>
    async (transaction, request, userAuthentication, me, myAdminAccess, fileAccess) =>
    {
      Resource<FileContent> fileContent = await Resources.GetMainFileContent(
        transaction,
        fileAccess.UnlockedFile.File
      );

      Resource<FileSnapshot> fileSnapshot = await Internal_EnsureFirst(
        transaction,
        Resources.Query<FileSnapshot>(
          transaction,
          (query) =>
            query
              .Where(
                (fileSnapshot) =>
                  (request.FileSnapshotId == null || fileSnapshot.Id == request.FileSnapshotId)
                  && fileSnapshot.FileId == fileAccess.UnlockedFile.File.Id
              )
              .OrderByDescending((fileSnapshot) => fileSnapshot.CreateTime)
        )
      );

      Resource<AudioTranscription> audioTranscription = await Server.AudioTranscriber.Process(
        transaction,
        fileAccess.UnlockedFile,
        fileContent,
        fileSnapshot
      );

      return new()
      {
        Text = audioTranscription.Data.Text.Select((token) => $"{token}").ToArray(),
        Status = audioTranscription.Data.Status,
      };
    };
}
