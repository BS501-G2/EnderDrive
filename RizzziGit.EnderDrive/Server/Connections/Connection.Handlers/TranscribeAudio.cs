using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

using Resources;

public sealed partial class Connection
{
  private sealed record class TranscribeAudioRequest : BaseFileRequest
  {
    public required ObjectId? FileDataId;
  }

  private sealed record class TranscribeAudioResponse
  {
    public required string[] Text;
    public required AudioTranscriptionStatus Status;
  }

  private FileRequestHandler<TranscribeAudioRequest, TranscribeAudioResponse> TranscribeAudio =>
    async (transaction, request, userAuthentication, me, myAdminAccess, fileAccess) =>
    {
      Resource<FileData> fileSnapshot = await Internal_EnsureFirst(
        transaction,
        Resources.Query<FileData>(
          transaction,
          (query) =>
            query
              .Where(
                (fileSnapshot) =>
                  (request.FileDataId == null || fileSnapshot.Id == request.FileDataId)
                  && fileSnapshot.FileId == fileAccess.UnlockedFile.File.Id
              )
              .OrderByDescending((fileSnapshot) => fileSnapshot.CreateTime)
        )
      );

      Resource<AudioTranscription> audioTranscription = await Server.AudioTranscriber.Process(
        transaction,
        fileAccess.UnlockedFile,
        fileSnapshot
      );

      return new()
      {
        Text = audioTranscription.Data.Text.Select((token) => token.Text).ToArray(),
        Status = audioTranscription.Data.Status,
      };
    };
}
