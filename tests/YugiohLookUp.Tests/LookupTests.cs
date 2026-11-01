using YugiohLookUp.Core.Application;

namespace YugiohLookUp.Tests;

public sealed class LookupTests
{
    private readonly Catalog catalog = new(TestCatalog.Document());

    [Theory]
    [InlineData("Spark - Knight")]
    [InlineData("spark - knight")]
    [InlineData("  Spark   Knight! ")]
    [InlineData("Electric Knight")]
    [InlineData("electric-knight")]
    public void ResolvesOfficialNormalizedAndAliasNames(string name) =>
        Assert.Equal("a", catalog.Resolve(name).Match?.Id);

    [Fact]
    public void DoesNotGuessUnknownOrAmbiguousNames()
    {
        Assert.Empty(catalog.Resolve("Unknown").Candidates);
        Assert.Null(catalog.Resolve("Twin-Beast").Match);
        Assert.True(catalog.Resolve("Twin-Beast").IsAmbiguous);
        Assert.Equal("c", catalog.Resolve("Twin: Beast").Match?.Id);
    }

    [Fact]
    public void ReturnsEveryContainingPackAndFiltersPackCards()
    {
        Assert.Equal(["p", "q"], catalog.PacksFor("a").Select(pack => pack.Id));
        Assert.Equal(["a", "b", "d"], catalog.CardsIn("q").Select(card => card.Id).Order());
        Assert.Equal("a", Assert.Single(catalog.CardsIn("q", "electric")).Id);
        Assert.Empty(catalog.PacksFor("u"));
        Assert.Empty(catalog.CardsIn("missing"));
    }

    [Fact]
    public void ReportsSearchTruncationAndMatchesAliases()
    {
        var results = catalog.Search("t", 1);
        Assert.Single(results.Cards);
        Assert.True(results.TotalCount > results.Cards.Count);
        Assert.Equal("a", Assert.Single(catalog.Search("electric").Cards).Id);
        Assert.Empty(catalog.Search("  ").Cards);
    }

    [Fact]
    public void NormalizationRetainsNumbersAndDistinguishesOtherSymbols()
    {
        Assert.Equal("NUMBER 2", NameNormalizer.Normalize("Number ２"));
        Assert.NotEqual(NameNormalizer.Normalize("A/B"), NameNormalizer.Normalize("AB"));
        Assert.Equal(NameNormalizer.Normalize("Knight's"), NameNormalizer.Normalize("Knight\u2019s"));
    }

    [Fact]
    public void ResolvesNonAsciiNamesAndFullWidthInputConsistently()
    {
        var document = TestCatalog.Document();
        var catalog = new Catalog(document with
        {
            Cards = [.. document.Cards, TestCatalog.Card("unicode", "Caf\u00e9 Knight")]
        });
        Assert.Equal("unicode", catalog.Resolve("Cafe\u0301 Knight").Match?.Id);
        Assert.Equal("b", catalog.Resolve("\uff24\uff52\uff41\uff47\uff4f\uff4e").Match?.Id);
    }
}
