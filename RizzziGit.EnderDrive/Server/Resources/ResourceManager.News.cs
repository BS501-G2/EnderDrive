using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using Newtonsoft.Json;

namespace RizzziGit.EnderDrive.Server.Resources;

public record class News : ResourceData
{
    [JsonProperty("title")]
    public required string Title;

    [JsonProperty("imageFileIds")]
    public required ObjectId[] ImageFileIds;

    [JsonProperty("authorUserId")]
    public required ObjectId AuthorUserId;
}

public sealed partial class ResourceManager
{
    public async Task<News> CreateNews(
        ResourceTransaction transaction,
        string title,
        File[] images,
        User newsAuthor
    )
    {
        News news =
            new()
            {
                Id = ObjectId.GenerateNewId(),
                Title = title,
                ImageFileIds = images.Select((item) => item.Id).ToArray(),
                AuthorUserId = newsAuthor.Id,
            };

        await Insert(transaction, news);
        return news;
    }

    public async Task DeleteNews(ResourceTransaction transaction, News news)
    {
        await Delete(transaction, news);
    }

    public IQueryable<News> GetNews(ResourceTransaction transaction) =>
        Query<News>(transaction, (query) => query.OrderByDescending((item) => item.Id));
}
