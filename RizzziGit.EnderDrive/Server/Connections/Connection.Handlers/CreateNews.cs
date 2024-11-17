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
    [BsonElement(
      "title"
    )]
    public required string Title;

    [BsonElement(
      "imageFileIds"
    )]
    public required ObjectId[] ImageFileIds;

    [BsonElement(
      "publishTime"
    )]
    [BsonRepresentation(
      BsonType.DateTime
    )]
    public required DateTimeOffset? PublishTime;
  }

  private sealed record CreateNewsResponse
  {
    [BsonElement(
      "news"
    )]
    public required string News;
  }

  private AuthenticatedRequestHandler<
    CreateNewsRequest,
    CreateNewsResponse
  > CreateNews =>
    async (
      transaction,
      request,
      userAuthentication,
      me,
      myAdminAccess
    ) =>
    {
      File[] files =
        await Task.WhenAll(
          request.ImageFileIds.Select(
            async (
              fileId
            ) =>
            {
              File file =
                await Internal_GetFile(
                  transaction,
                  me,
                  fileId
                );
              FileAccess fileAccess =
                await Resources
                  .GetFileAccesses(
                    transaction,
                    targetFile: file
                  )
                  .Where(
                    (
                      item
                    ) =>
                      item.TargetEntity
                      == null
                  )
                  .ToAsyncEnumerable()
                  .FirstOrDefaultAsync(
                    transaction
                  )
                ?? throw new InvalidOperationException(
                  "Images must be public."
                );

              if (
                file.Type
                != FileType.File
              )
              {
                throw new InvalidOperationException(
                  "File is not a file"
                );
              }

              if (
                file.TrashTime
                != null
              )
              {
                throw new InvalidOperationException(
                  "File is in the trash"
                );
              }

              FileContent fileContent =
                await Resources.GetMainFileContent(
                  transaction,
                  file
                );

              FileSnapshot fileSnapshot =
                await Resources.GetLatestFileSnapshot(
                  transaction,
                  file,
                  fileContent
                )
                ?? await Resources.CreateFileSnapshot(
                  transaction,
                  file.WithAesKey(
                    fileAccess.EncryptedAesKey
                  ),
                  fileContent,
                  userAuthentication,
                  null
                );

              Definition? mimeResult =
                await Server.MimeDetector.Inspect(
                  transaction,
                  file.WithAesKey(
                    fileAccess.EncryptedAesKey
                  ),
                  fileContent,
                  fileSnapshot
                );

              if (
                mimeResult
                  == null
                || mimeResult
                  .File
                  .MimeType
                  == null
                || !mimeResult.File.MimeType.StartsWith(
                  "image/"
                )
              )
              {
                throw new InvalidOperationException(
                  "File is not an image"
                );
              }

              return file;
            }
          )
        );

      News news =
        await Resources.CreateNews(
          transaction,
          request.Title,
          files,
          me,
          request.PublishTime
        );

      return new()
      {
        News =
          news.ToJson(),
      };
    };
}
