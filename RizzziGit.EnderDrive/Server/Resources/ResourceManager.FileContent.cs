using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;

namespace RizzziGit.EnderDrive.Server.Resources;

public record class FileContent : ResourceData
{
    [JsonProperty("fileId")]
    public required ObjectId FileId;

    [JsonProperty("main")]
    public required bool Main;

    [JsonProperty("name")]
    public required string Name;
}

public sealed partial class ResourceManager
{
    public async Task<FileContent> GetMainFileContent(ResourceTransaction transaction, File file)
    {
        FileContent? content = await Query<FileContent>(
                transaction,
                (query) => query.Where((item) => item.FileId == file.Id && item.Main)
            )
            .ToAsyncEnumerable()
            .FirstOrDefaultAsync(transaction.CancellationToken);

        if (content != null)
        {
            return content;
        }

        await Insert(
            transaction,
            content = new()
            {
                Id = ObjectId.GenerateNewId(),

                FileId = file.Id,
                Main = true,
                Name = "Main Content",
            }
        );

        return content;
    }

    public async Task<FileContent> CreateFileContent(
        ResourceTransaction transaction,
        UnlockedFile file,
        string name
    )
    {
        FileContent content =
            new()
            {
                Id = ObjectId.GenerateNewId(),
                FileId = file.Id,
                Main = false,
                Name = name,
            };

        await Insert(transaction, content);

        return content;
    }

    public IQueryable<FileContent> GetFileContents(
        ResourceTransaction transaction,
        File file,
        string? name = null
    ) =>
        Query<FileContent>(
            transaction,
            (query) =>
                query.Where(
                    (item) =>
                        item.FileId == file.Id
                        && (
                            name == null
                            || file.Name.Contains(
                                name,
                                System.StringComparison.CurrentCultureIgnoreCase
                            )
                        )
                )
        );
}
