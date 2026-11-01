using YugiohLookUp.Core.Application;
using YugiohLookUp.Import;

namespace YugiohLookUp.Tests;

public sealed class PackTextTests
{
    [Fact]
    public void ParsesSectionsWithWhitespaceAndMixedLineEndingsAndKeepsAllPacks()
    {
        var result = PackTextReader.Parse(" Pack: Alpha \r\n Dragon \r\nNumber 2\n\npack: Beta\rDragon\r", "Test");
        Assert.Empty(result.Diagnostics);
        var catalog = new Catalog(result.Document);
        Assert.Equal(2, catalog.Cards.Count);
        Assert.Equal(["Alpha", "Beta"], catalog.PacksFor(catalog.Resolve("Dragon").Match!.Id).Select(pack => pack.Name));
        Assert.Equal("Number 2", catalog.Resolve("Number 2").Match?.Name);
        Assert.All(result.Document.Packs, pack => Assert.True(CatalogValidator.IsAssetPath(pack.ImagePath)));
        Assert.False(result.Document.IsDemo);
        Assert.Contains("SHA-256", result.Document.Source);
    }

    [Fact]
    public void DeduplicatesRepeatedCardNamesWithLineWarningsButNotNormalizationCollisions()
    {
        var result = PackTextReader.Parse("Pack: Alpha\nDragon\ndragon\nTwin: Beast\nTwin Beast", "Test");
        Assert.Equal(3, result.Document.Cards.Count);
        Assert.Equal(3, result.Document.CardPacks.Count);
        var warning = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
        Assert.Equal("line 3", warning.Location);
        Assert.Contains("line 2", warning.Message);
        Assert.True(new Catalog(result.Document).Resolve("Twin-Beast").IsAmbiguous);
    }

    [Fact]
    public void StableIdsDoNotDependOnOrderLineEndingsOrUnrelatedAdditions()
    {
        var first = PackTextReader.Parse("Pack: Alpha\nDragon\nNumber 2", "Test");
        var second = PackTextReader.Parse("Pack: Beta\r\nExtra\r\nPack: alpha\r\ndragon\r\nNumber 2", "Test");
        Assert.Equal(first.Document.Cards.First(card => card.Name == "Dragon").Id,
            second.Document.Cards.First(card => card.Name == "dragon").Id);
        Assert.Equal(first.Document.Packs[0].Id, second.Document.Packs[1].Id);
    }

    [Theory]
    [InlineData("Dragon\nPack: Alpha\nDragon", "card-without-pack", "line 1")]
    [InlineData("Pack:\nDragon", "pack-heading", "line 1")]
    [InlineData("Pack: Alpha\nPack: Beta\nDragon", "empty-pack", "line 1")]
    [InlineData("Pack: Alpha\nDragon\nPack: Beta", "empty-pack", "line 3")]
    [InlineData("Pack: Alpha\nDragon\nPack: alpha\nDragon", "duplicate-pack", "line 3")]
    public void ReportsStructuralErrorsAtSourceLines(string text, string code, string location)
    {
        var result = PackTextReader.Parse(text, "Test");
        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Severity == DiagnosticSeverity.Error && diagnostic.Code == code && diagnostic.Location == location);
    }
}
