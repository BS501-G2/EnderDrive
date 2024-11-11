using System;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

using Newtonsoft.Json.Linq;
using Resources;
using Utilities;

public sealed partial class Connection
{
    private sealed record CreateNewsRequest
    {
        [BsonElement("title")]
        public required string Title;

        [BsonElement("imageFileIds")]
        public required ObjectId[] ImageFileIds;
    }

    private sealed record CreateNewsResponse
    {
        [BsonElement("news")]
        public required string News;
    }

    private AuthenticatedRequestHandler<CreateNewsRequest, CreateNewsResponse> CreateNews =>
        async (transaction, request, userAuthentication, me) =>
        {
            File[] files = await Task.WhenAll(
                request.ImageFileIds.Select(
                    async (fileId) =>
                    {
                        File file = await Internal_GetFile(transaction, me, fileId);

                        if (file.Type != FileType.File)
                        {
                            throw new InvalidOperationException("File is not a file");
                        }

                        if (file.TrashTime != null)
                        {
                            throw new InvalidOperationException("File is in the trash");
                        }

                        return file;
                    }
                )
            );

            News news = await Resources.CreateNews(transaction, request.Title, files, me);

            return new() { News = news.ToJson() };
        };
}
