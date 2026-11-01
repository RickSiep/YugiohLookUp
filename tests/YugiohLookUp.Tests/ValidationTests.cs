using System.Text.Json;
using YugiohLookUp.Core.Application;
using YugiohLookUp.Core.Domain;

namespace YugiohLookUp.Tests;

public sealed class ValidationTests
{
    [Fact]
    public void ReportsDuplicateIdsAndRelationships()
    {
        var document = TestCatalog.Document();
        document = document with
        {
            Cards = [.. document.Cards, document.Cards[0]],
            Packs = [.. document.Packs, document.Packs[0]],
            CardPacks = [.. document.CardPacks, document.CardPacks[0]]
        };
        var diagnostics = CatalogValidator.Validate(document);
        Assert.Contains(diagnostics, d => d.Code == "duplicate-card");
        Assert.Contains(diagnostics, d => d.Code == "duplicate-pack");
        Assert.Contains(diagnostics, d => d.Code == "duplicate-relationship");
        Assert.Throws<InvalidDataException>(() => new Catalog(document));
    }

    [Fact]
    public void ReportsMissingNamesEndpointsSchemaAndCollisions()
    {
        var document = TestCatalog.Document() with
        {
            SchemaVersion = 9,
            Cards = [TestCatalog.Card("a", ""), TestCatalog.Card("b", "Collision"), TestCatalog.Card("c", "Collision!")],
            CardPacks = [new() { CardId = "missing", PackId = "absent" }]
        };
        var diagnostics = CatalogValidator.Validate(document);
        Assert.Contains(diagnostics, d => d.Code == "schema");
        Assert.Contains(diagnostics, d => d.Code == "card");
        Assert.Contains(diagnostics, d => d.Code == "relationship");
        Assert.Contains(diagnostics, d => d.Code == "name-collision" && d.Severity == DiagnosticSeverity.Warning);
    }

    [Fact]
    public void MissingSchemaVersionIsNotImplicitlyAccepted()
    {
        var json = JsonSerializer.Serialize(TestCatalog.Document(), CatalogJsonContext.Default.CatalogDocument);
        json = json.Replace("\"schemaVersion\": 1,", "", StringComparison.Ordinal);
        var document = JsonSerializer.Deserialize(json, CatalogJsonContext.Default.CatalogDocument)!;
        Assert.Contains(CatalogValidator.Validate(document), d => d.Code == "schema");
    }

    [Theory]
    [InlineData("https://example.org/p.webp")]
    [InlineData("/assets/p.webp")]
    [InlineData("assets/../p.webp")]
    [InlineData("assets/%2e%2e/p.webp")]
    [InlineData("assets\\p.webp")]
    [InlineData("assets/p.webp?v=1")]
    [InlineData("assets//p.webp")]
    public void RejectsUnsafeAssetPaths(string path) =>
        Assert.False(CatalogValidator.IsAssetPath(path));

    [Theory]
    [InlineData("""{"datasetVersion":"1","source":"test","cards":null,"packs":[],"cardPacks":[]}""")]
    [InlineData("""{"datasetVersion":"1","source":"test","cards":[null],"packs":[null],"cardPacks":[null]}""")]
    [InlineData("""{"datasetVersion":"1","source":"test","cards":[{"id":"a","name":null,"aliases":null}],"packs":[],"cardPacks":[]}""")]
    public void CorruptJsonProducesDiagnosticsNotNullReferenceFailures(string json)
    {
        var document = JsonSerializer.Deserialize(json, CatalogJsonContext.Default.CatalogDocument)!;
        Assert.Contains(CatalogValidator.Validate(document), d => d.Severity == DiagnosticSeverity.Error);
        Assert.Throws<InvalidDataException>(() => new Catalog(document));
    }
}
