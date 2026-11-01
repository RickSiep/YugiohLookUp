using YugiohLookUp.Core.Application;

namespace YugiohLookUp.Tests;

public sealed class DeckTests
{
    private readonly Catalog catalog = new(TestCatalog.Document());

    [Theory]
    [InlineData("Dragon", 1)]
    [InlineData("Dragon x2", 2)]
    [InlineData("Dragon X3", 3)]
    [InlineData("Dragon x 2", 2)]
    [InlineData("Dragon 2", 2)]
    [InlineData("2 Dragon", 2)]
    [InlineData("2x Dragon", 2)]
    [InlineData("2X Dragon", 2)]
    public void ParsesSupportedQuantities(string input, int quantity)
    {
        var line = Assert.Single(DeckParser.Parse(input, catalog));
        Assert.Null(line.Error);
        Assert.Equal("Dragon", line.Name);
        Assert.Equal(quantity, line.Quantity);
    }

    [Theory]
    [InlineData("Dragon x0")]
    [InlineData("Dragon -2")]
    [InlineData("-2 Dragon")]
    [InlineData("Dragon x2147483648")]
    [InlineData("Dragon xno")]
    [InlineData("2")]
    [InlineData("2x")]
    [InlineData("Dragon x")]
    [InlineData("Dragon x2 x3")]
    public void ReportsInvalidLines(string input) =>
        Assert.NotNull(Assert.Single(DeckParser.Parse(input, catalog)).Error);

    [Fact]
    public void IgnoresBlanksPreservesLineNumbersAndProtectsNumericNames()
    {
        var lines = DeckParser.Parse("\r\n Number 2\r\n \nDragon 3\n", catalog);
        Assert.Equal(2, lines.Count);
        Assert.Equal(2, lines[0].LineNumber);
        Assert.Equal("Number 2", lines[0].Name);
        Assert.Equal(1, lines[0].Quantity);
        Assert.Equal(3, lines[1].Quantity);
    }

    [Fact]
    public void MergesAliasesGroupsEveryPackAndPreservesProblems()
    {
        var result = DeckLookup.Lookup("Spark Knight x2\nElectric Knight\nDragon x2\nMissing x4\nUncatalogued", catalog);
        Assert.Equal(3, result.Matched.Single(item => item.Card.Id == "a").Quantity);
        Assert.Equal("Missing", Assert.Single(result.Issues).Line.Name);
        Assert.Equal(4, result.Issues[0].Line.Quantity);
        Assert.Equal("q", result.Packs[0].Pack.Id);
        Assert.Equal(2, result.Packs[0].DistinctCards);
        Assert.Equal(5, result.Packs[0].RequestedCopies);
        Assert.Equal("p", result.Packs[1].Pack.Id);
        Assert.Equal("u", Assert.Single(result.WithoutPacks).Card.Id);
    }

    [Fact]
    public void RequiresValidExplicitAmbiguityChoice()
    {
        var result = DeckLookup.Lookup("Twin-Beast x2", catalog);
        Assert.Empty(result.Matched);
        Assert.Equal(2, Assert.Single(result.Issues).Candidates.Count);
        Assert.Empty(DeckLookup.Lookup("Twin-Beast x2", catalog, new Dictionary<int, string> { [1] = "wrong" }).Matched);
        Assert.Equal("d", Assert.Single(DeckLookup.Lookup("Twin-Beast x2", catalog,
            new Dictionary<int, string> { [1] = "d" }).Matched).Card.Id);
    }

    [Fact]
    public void QuantityOverflowDoesNotDiscardEarlierValidLines()
    {
        var result = DeckLookup.Lookup("Dragon x2147483647\nDragon", catalog);
        Assert.Equal(int.MaxValue, Assert.Single(result.Matched).Quantity);
        Assert.Contains("Combined quantity", Assert.Single(result.Issues).Message);
    }
}
