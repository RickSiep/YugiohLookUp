using YugiohLookUp.Core.Application;
using YugiohLookUp.Core.Domain;

namespace YugiohLookUp.Tests;

internal static class TestCatalog
{
    public static Card Card(string id, string name, params string[] aliases) => new()
    {
        Id = id,
        Name = name,
        NormalizedName = NameNormalizer.Normalize(name),
        Aliases = aliases
    };

    public static CatalogDocument Document() => new()
    {
        SchemaVersion = 1,
        DatasetVersion = "test-1",
        Source = "Synthetic tests",
        Cards =
        [
            Card("a", "Spark - Knight", "Electric Knight"),
            Card("b", "Dragon"),
            Card("n", "Number 2"),
            Card("c", "Twin: Beast"),
            Card("d", "Twin Beast"),
            Card("u", "Uncatalogued")
        ],
        Packs =
        [
            new() { Id = "p", Name = "Alpha", ImagePath = "assets/packs/alpha.webp" },
            new() { Id = "q", Name = "Beta", ImagePath = "assets/packs/beta.jpg" }
        ],
        CardPacks =
        [
            new() { CardId = "a", PackId = "p" },
            new() { CardId = "a", PackId = "q" },
            new() { CardId = "b", PackId = "q" },
            new() { CardId = "n", PackId = "p" },
            new() { CardId = "c", PackId = "p" },
            new() { CardId = "d", PackId = "q" }
        ]
    };
}
