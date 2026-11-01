using YugiohLookUp.Core.Domain;

namespace YugiohLookUp.Core.Application;

public sealed record CardResolution(IReadOnlyList<Card> Candidates)
{
    public Card? Match => Candidates.Count == 1 ? Candidates[0] : null;
    public bool IsAmbiguous => Candidates.Count > 1;
}

public sealed record CardSearchResult(IReadOnlyList<Card> Cards, int TotalCount);

public sealed class Catalog
{
    private readonly Dictionary<string, Card[]> exact;
    private readonly Dictionary<string, Card[]> normalized;
    private readonly Dictionary<string, Card[]> aliases;
    private readonly Dictionary<string, Pack[]> cardPacks;
    private readonly Dictionary<string, Card[]> packCards;
    private readonly Dictionary<string, string[]> searchNames;
    public IReadOnlyList<Card> Cards { get; }
    public IReadOnlyList<Pack> Packs { get; }
    public string DatasetVersion { get; }
    public bool IsDemo { get; }

    public Catalog(CatalogDocument document)
    {
        var errors = CatalogValidator.Validate(document).Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        if (errors.Length != 0)
            throw new InvalidDataException(string.Join(Environment.NewLine, errors.Select(d => $"{d.Location}: {d.Message}")));

        Cards = Array.AsReadOnly(document.Cards
            .Select(card => card with { Aliases = Array.AsReadOnly(card.Aliases.ToArray()) })
            .OrderBy(card => card.Name, StringComparer.OrdinalIgnoreCase).ThenBy(card => card.Id, StringComparer.Ordinal).ToArray());
        Packs = Array.AsReadOnly(document.Packs.OrderBy(pack => pack.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(pack => pack.Id, StringComparer.Ordinal).ToArray());
        DatasetVersion = document.DatasetVersion;
        IsDemo = document.IsDemo;
        searchNames = Cards.ToDictionary(card => card.Id,
            card => new[] { card.NormalizedName }.Concat(card.Aliases.Select(NameNormalizer.Normalize)).ToArray(),
            StringComparer.Ordinal);
        exact = Cards.GroupBy(card => card.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.OrdinalIgnoreCase);
        normalized = Cards.GroupBy(card => card.NormalizedName, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
        aliases = Cards.SelectMany(card => card.Aliases.Select(alias => (Name: NameNormalizer.Normalize(alias), Card: card)))
            .GroupBy(pair => pair.Name, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Select(pair => pair.Card).DistinctBy(card => card.Id).ToArray(), StringComparer.Ordinal);
        var cardsById = Cards.ToDictionary(card => card.Id, StringComparer.Ordinal);
        var packsById = Packs.ToDictionary(pack => pack.Id, StringComparer.Ordinal);
        cardPacks = document.CardPacks.GroupBy(link => link.CardId).ToDictionary(group => group.Key,
            group => group.Select(link => packsById[link.PackId]).OrderBy(pack => pack.Name, StringComparer.OrdinalIgnoreCase).ToArray());
        packCards = document.CardPacks.GroupBy(link => link.PackId).ToDictionary(group => group.Key,
            group => group.Select(link => cardsById[link.CardId]).OrderBy(card => card.Name, StringComparer.OrdinalIgnoreCase).ToArray());
    }

    public CardResolution Resolve(string name)
    {
        if (exact.TryGetValue(name.Trim(), out var matches) ||
            normalized.TryGetValue(NameNormalizer.Normalize(name), out matches) ||
            aliases.TryGetValue(NameNormalizer.Normalize(name), out matches))
            return new(Array.AsReadOnly(matches));
        return new([]);
    }

    public CardSearchResult Search(string query, int limit = 50)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);
        var key = NameNormalizer.Normalize(query);
        if (key.Length == 0)
            return new([], 0);
        var matches = Cards.Where(card => Matches(card, key)).ToArray();
        return new(Array.AsReadOnly(matches.Take(limit).ToArray()), matches.Length);
    }

    public IReadOnlyList<Pack> PacksFor(string cardId) =>
        cardPacks.TryGetValue(cardId, out var packs) ? Array.AsReadOnly(packs) : [];

    public IReadOnlyList<Card> CardsIn(string packId, string filter = "")
    {
        if (!packCards.TryGetValue(packId, out var cards))
            return [];
        var key = NameNormalizer.Normalize(filter);
        return Array.AsReadOnly(cards.Where(card => key.Length == 0 || Matches(card, key)).ToArray());
    }

    private bool Matches(Card card, string key) =>
        searchNames[card.Id].Any(name => name.Contains(key, StringComparison.Ordinal));
}
