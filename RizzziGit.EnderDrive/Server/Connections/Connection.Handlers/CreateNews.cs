using System;
using System.Linq;
using System.Threading.Tasks;
using MimeDetective.Storage;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

using Resources;
using FileType = Resources.FileType;

public sealed partial class Connection
{
  private sealed record CreateNewsRequest
  {
    public required string Title;
    public required ObjectId ImageFileId;
    public required long? PublishTime;
  }

  private sealed record CreateNewsResponse
  {
    public required string News;
  }

  private AuthenticatedRequestHandler<CreateNewsRequest, CreateNewsResponse> CreateNews =>
    async (transaction, request, userAuthentication, me, myAdminAccess) =>
    {
      Resource<File> file = await Internal_GetFile(
        transaction,
        me,
        userAuthentication,
        request.ImageFileId
      );

      FileAccessResult fileAccessResult = await Internal_UnlockFile(
        transaction,
        file,
        me,
        userAuthentication,
        FileAccessLevel.Read
      );

      Resource<FileAccess> fileAccess =
        await Resources
          .Query<FileAccess>(
            transaction,
            (query) => query.Where((item) => item.FileId == file.Id && item.TargetUserId == null)
          )
          .FirstOrDefaultAsync(transaction)
        ?? throw new InvalidOperationException("The image must be public.");

      if (file.Data.Type != FileType.File)
      {
        throw new InvalidOperationException("File is not a type of file.");
      }

      if (file.Data.TrashTime != null)
      {
        throw new InvalidOperationException("File is in the trash.");
      }

      Resource<FileData> fileSnapshot =
        await Resources
          .Query<FileData>(
            transaction,
            (query) =>
              query.Where((item) => item.FileId == file.Id).OrderByDescending((item) => item.Id)
          )
          .FirstOrDefaultAsync(transaction.CancellationToken)
        ?? throw new InvalidOperationException("File does not yet have content.");

      Definition? mimeResult = await Server.MimeDetector.Inspect(
        transaction,
        UnlockedFile.WithAesKey(file, fileAccess.Data.EncryptedAesKey),
        fileSnapshot
      );

      if (
        mimeResult == null
        || mimeResult.File.MimeType == null
        || !mimeResult.File.MimeType.StartsWith("image/")
      )
      {
        throw new InvalidOperationException("File is not an image");
      }

      Resource<News> news = await Resources.CreateNews(
        transaction,
        request.Title,
        fileAccessResult.UnlockedFile,
        fileSnapshot,
        me,
        request.PublishTime
      );

      return new() { News = news.ToJson() };
    };
}
