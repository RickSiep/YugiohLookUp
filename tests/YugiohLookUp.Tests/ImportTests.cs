using System.Text.Json;
using YugiohLookUp.Core.Domain;
using YugiohLookUp.Import;

namespace YugiohLookUp.Tests;

public sealed class ImportTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "arcv-import-tests-" + Guid.NewGuid().ToString("N"));

    public ImportTests() => Directory.CreateDirectory(root);

    [Fact]
    public async Task DraftImportNormalizesNamesAndIsDeterministic()
    {
        var document = TestCatalog.Document();
        var first = await Run(document with { Cards = document.Cards.Reverse().Select(card => card with { NormalizedName = "" }).ToArray() });
        Assert.Equal(0, first.Code);
        Assert.Contains("missing-asset", first.Log);
        var bytes = await File.ReadAllBytesAsync(Path.Combine(root, "output.json"));
        var second = await Run(document);
        Assert.Equal(0, second.Code);
        Assert.Equal(bytes, await File.ReadAllBytesAsync(Path.Combine(root, "output.json")));
    }

    [Fact]
    public async Task InvalidInputDoesNotOverwritePreviousOutput()
    {
        await File.WriteAllTextAsync(Path.Combine(root, "output.json"), "previous");
        var result = await Run(TestCatalog.Document() with
        {
            CardPacks = [new() { CardId = "bad", PackId = "bad" }]
        });
        Assert.Equal(1, result.Code);
        Assert.Contains("relationship", result.Log);
        Assert.Equal("previous", await File.ReadAllTextAsync(Path.Combine(root, "output.json")));
    }

    [Fact]
    public async Task DiagnosticsReferenceOriginalInputOrder()
    {
        var document = TestCatalog.Document();
        var result = await Run(document with
        {
            Cards = [TestCatalog.Card("z", "Valid"), TestCatalog.Card("a", "")]
        });
        Assert.Equal(1, result.Code);
        Assert.Contains("Error card [cards[1]]", result.Log);
    }

    [Fact]
    public async Task StrictImportRequiresRealPackAssetsAndRejectsDemo()
    {
        Assert.Equal(1, (await Run(TestCatalog.Document(), true)).Code);
        Directory.CreateDirectory(Path.Combine(root, "assets", "packs"));
        await File.WriteAllTextAsync(Path.Combine(root, "assets", "packs", "alpha.webp"), "test asset");
        await File.WriteAllTextAsync(Path.Combine(root, "assets", "packs", "beta.jpg"), "test asset");
        Assert.Equal(0, (await Run(TestCatalog.Document(), true)).Code);
        Assert.Equal(1, (await Run(TestCatalog.Document() with { IsDemo = true }, true)).Code);
        var document = TestCatalog.Document();
        Assert.Equal(1, (await Run(document with
        {
            Packs = document.Packs.Select(pack => pack with { ImagePath = "assets/placeholder.svg" }).ToArray()
        }, true)).Code);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{")]
    [InlineData("""{"cards":null,"packs":null,"cardPacks":null}""")]
    public async Task MalformedInputReturnsUsefulFailure(string json)
    {
        await File.WriteAllTextAsync(Path.Combine(root, "input.json"), json);
        using var log = new StringWriter();
        var code = await ImportRunner.RunAsync([Path.Combine(root, "input.json"), Path.Combine(root, "output.json"), root], log);
        Assert.Equal(1, code);
        Assert.Contains("Error", log.ToString());
        Assert.False(File.Exists(Path.Combine(root, "output.json")));
    }

    [Fact]
    public async Task TextImportRequiresSourceAndRejectsInvalidSectionsWithoutOverwriting()
    {
        var input = Path.Combine(root, "input.txt");
        var destination = Path.Combine(root, "output.json");
        await File.WriteAllTextAsync(input, "Pack: Alpha\nDragon\nPack: Beta\nDragon");
        using var log = new StringWriter();
        Assert.Equal(2, await ImportRunner.RunAsync([input, destination, root], log));
        Assert.False(File.Exists(destination));
        Assert.Equal(0, await ImportRunner.RunAsync([input, destination, root, "--source", "Synthetic test"], log));
        var previous = await File.ReadAllTextAsync(destination);
        var document = JsonSerializer.Deserialize(previous, CatalogJsonContext.Default.CatalogDocument)!;
        Assert.Single(document.Cards);
        Assert.Equal(2, document.CardPacks.Count);
        Assert.Contains("Synthetic test", document.Source);
        Assert.Equal(1, await ImportRunner.RunAsync([input, destination, root, "--strict", "--source", "Synthetic test"], log));
        Assert.Equal(previous, await File.ReadAllTextAsync(destination));
        await File.WriteAllTextAsync(input, "Pack: Alpha\nDragon\nPack: Empty");
        Assert.Equal(1, await ImportRunner.RunAsync([input, destination, root, "--source", "Synthetic test"], log));
        Assert.Equal(previous, await File.ReadAllTextAsync(destination));
        Assert.Contains("empty-pack [line 3]", log.ToString());
    }

    private async Task<(int Code, string Log)> Run(CatalogDocument document, bool strict = false)
    {
        await File.WriteAllTextAsync(Path.Combine(root, "input.json"), JsonSerializer.Serialize(document, CatalogJsonContext.Default.CatalogDocument));
        var args = new List<string> { Path.Combine(root, "input.json"), Path.Combine(root, "output.json"), root };
        if (strict) args.Add("--strict");
        using var log = new StringWriter();
        return (await ImportRunner.RunAsync(args.ToArray(), log), log.ToString());
    }

    public void Dispose() => Directory.Delete(root, recursive: true);
}
