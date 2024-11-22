using System;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

using MimeDetective.Storage;
using Newtonsoft.Json.Linq;
using Resources;
using Utilities;
using FileType = Resources.FileType;

public sealed partial class Connection
{
  private sealed record CreateNewsRequest
  {
    [BsonElement("title")]
    public required string Title;

    [BsonElement("imageFileIds")]
    public required ObjectId[] ImageFileIds;

    [BsonElement("publishTime")]
    [BsonRepresentation(BsonType.DateTime)]
    public required DateTimeOffset? PublishTime;
  }

  private sealed record CreateNewsResponse
  {
    [BsonElement("news")]
    public required string News;
  }

  private AuthenticatedRequestHandler<CreateNewsRequest, CreateNewsResponse> CreateNews =>
    async (transaction, request, userAuthentication, me, myAdminAccess) =>
    {
      Resource<File>[] files = await Task.WhenAll(
        request.ImageFileIds.Select(
          async (fileId) =>
          {
            Resource<File> file = await Internal_GetFile(
              transaction,
              me,
              userAuthentication,
              fileId
            );
            Resource<FileAccess> fileAccess =
              await Resources
                .Query<FileAccess>(
                  transaction,
                  (query) =>
                    query.Where((item) => item.FileId == file.Id && item.TargetUserId == null)
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

            Resource<FileContent> fileContent = await Resources.GetMainFileContent(
              transaction,
              file
            );

            Resource<FileSnapshot> fileSnapshot =
              await Resources
                .Query<FileSnapshot>(
                  transaction,
                  (query) =>
                    query
                      .Where((item) => item.FileId == file.Id)
                      .OrderByDescending((item) => item.Id)
                )
                .FirstOrDefaultAsync(transaction.CancellationToken)
              ?? await Resources.CreateFileSnapshot(
                transaction,
                UnlockedFile.WithAesKey(file, fileAccess.Data.EncryptedAesKey),
                fileContent,
                userAuthentication,
                null
              );

            Definition? mimeResult = await Server.MimeDetector.Inspect(
              transaction,
              UnlockedFile.WithAesKey(file, fileAccess.Data.EncryptedAesKey),
              fileContent,
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

            return file;
          }
        )
      );

      Resource<News> news = await Resources.CreateNews(
        transaction,
        request.Title,
        files,
        me,
        request.PublishTime
      );

      return new() { News = news.ToJson() };
    };
}
