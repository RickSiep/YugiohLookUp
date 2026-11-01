using System.Text.Json;
using YugiohLookUp.Core.Application;
using YugiohLookUp.Core.Domain;

namespace YugiohLookUp.Web.Infrastructure;

public sealed class CatalogStore(HttpClient http, ILogger<CatalogStore> logger)
{
    private Task<Catalog>? initialization;

    public Task<Catalog> LoadAsync() => initialization ??= LoadCoreAsync();

    public Task<Catalog> RetryAsync()
    {
        if (initialization is null || initialization.IsFaulted || initialization.IsCanceled)
            initialization = LoadCoreAsync();
        return initialization;
    }

    private async Task<Catalog> LoadCoreAsync()
    {
        try
        {
            using var response = await http.GetAsync("data/catalog.json");
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync();
            var document = await JsonSerializer.DeserializeAsync(stream, CatalogJsonContext.Default.CatalogDocument)
                ?? throw new InvalidDataException("Catalog is empty.");
            return new Catalog(document);
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or IOException or InvalidDataException or TaskCanceledException)
        {
            logger.LogError(exception, "Unable to load the bundled catalog");
            throw;
        }
    }
}
