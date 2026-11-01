using System.Globalization;
using System.Text.RegularExpressions;
using YugiohLookUp.Core.Domain;

namespace YugiohLookUp.Core.Application;

public sealed record DeckLine(int LineNumber, string Original, string Name, int Quantity, string? Error);
public sealed record RequestedCard(Card Card, int Quantity);
public sealed record DeckIssue(DeckLine Line, string Message, IReadOnlyList<Card> Candidates);
public sealed record PackGroup(Pack Pack, IReadOnlyList<RequestedCard> Cards)
{
    public int DistinctCards => Cards.Count;
    public long RequestedCopies => Cards.Sum(card => (long)card.Quantity);
}
public sealed record DeckLookupResult(
    IReadOnlyList<RequestedCard> Matched,
    IReadOnlyList<DeckIssue> Issues,
    IReadOnlyList<PackGroup> Packs,
    IReadOnlyList<RequestedCard> WithoutPacks);

public static partial class DeckParser
{
    [GeneratedRegex(@"^(?<name>.+?)\s+[xX]\s*(?<qty>[+-]?\d+)$")]
    private static partial Regex SuffixX();
    [GeneratedRegex(@"^(?<qty>[+-]?\d+)\s*[xX]\s+(?<name>.+)$")]
    private static partial Regex PrefixX();
    [GeneratedRegex(@"^(?<qty>[+-]?\d+)\s+(?<name>.+)$")]
    private static partial Regex PrefixNumber();
    [GeneratedRegex(@"^(?<name>.+?)\s+(?<qty>[+-]?\d+)$")]
    private static partial Regex SuffixNumber();
    [GeneratedRegex(@"(^[+-]?\d+\s*[xX]?\s*$)|(\s+[xX](\s*\S*)$)|(^[xX]\d+\s)|(^[+-]?\d+[xX]\S)")]
    private static partial Regex MalformedQuantity();

    public static IReadOnlyList<DeckLine> Parse(string text, Catalog catalog)
    {
        var lines = new List<DeckLine>();
        using var reader = new StringReader(text);
        var number = 0;
        while (reader.ReadLine() is { } original)
        {
            number++;
            var value = original.Trim();
            if (value.Length == 0)
                continue;
            if (catalog.Resolve(value).Candidates.Count > 0)
            {
                lines.Add(new(number, original, value, 1, null));
                continue;
            }
            var match = SuffixX().Match(value);
            if (!match.Success) match = PrefixX().Match(value);
            if (!match.Success) match = PrefixNumber().Match(value);
            if (!match.Success) match = SuffixNumber().Match(value);
            if (match.Success)
            {
                var name = match.Groups["name"].Value.Trim();
                if (!int.TryParse(match.Groups["qty"].Value, NumberStyles.AllowLeadingSign,
                    CultureInfo.InvariantCulture, out var quantity) || quantity <= 0)
                    lines.Add(new(number, original, name, 0, "Quantity must be a positive integer no greater than 2147483647."));
                else if (MalformedQuantity().IsMatch(name))
                    lines.Add(new(number, original, name, quantity, "Use only one quantity and provide a card name."));
                else
                    lines.Add(new(number, original, name, quantity, null));
            }
            else
                lines.Add(new(number, original, value, 1,
                    MalformedQuantity().IsMatch(value) ? "Invalid quantity syntax or missing card name." : null));
        }
        return lines.AsReadOnly();
    }
}

public static class DeckLookup
{
    public static DeckLookupResult Lookup(string text, Catalog catalog, IReadOnlyDictionary<int, string>? choices = null)
    {
        var matched = new Dictionary<string, RequestedCard>(StringComparer.Ordinal);
        var issues = new List<DeckIssue>();
        foreach (var line in DeckParser.Parse(text, catalog))
        {
            if (line.Error is not null)
            {
                issues.Add(new(line, line.Error, []));
                continue;
            }
            var resolution = catalog.Resolve(line.Name);
            var card = resolution.Match;
            if (resolution.IsAmbiguous && choices is not null && choices.TryGetValue(line.LineNumber, out var id))
                card = resolution.Candidates.FirstOrDefault(candidate => candidate.Id == id);
            if (card is null)
            {
                issues.Add(new(line, resolution.IsAmbiguous ? "Ambiguous name: choose a card." : "Card not found.", resolution.Candidates));
                continue;
            }
            var previous = matched.GetValueOrDefault(card.Id)?.Quantity ?? 0;
            if ((long)previous + line.Quantity > int.MaxValue)
            {
                issues.Add(new(line, "Combined quantity exceeds 2147483647; this line was not added.", []));
                continue;
            }
            matched[card.Id] = new(card, previous + line.Quantity);
        }
        var cards = matched.Values.OrderBy(item => item.Card.Name, StringComparer.OrdinalIgnoreCase).ToArray();
        var packs = cards.SelectMany(item => catalog.PacksFor(item.Card.Id).Select(pack => (Pack: pack, Card: item)))
            .GroupBy(pair => pair.Pack.Id)
            .Select(group => new PackGroup(group.First().Pack, Array.AsReadOnly(group.Select(pair => pair.Card).ToArray())))
            .OrderByDescending(group => group.DistinctCards).ThenBy(group => group.Pack.Name, StringComparer.OrdinalIgnoreCase).ToArray();
        return new(Array.AsReadOnly(cards), issues.AsReadOnly(), Array.AsReadOnly(packs),
            Array.AsReadOnly(cards.Where(item => catalog.PacksFor(item.Card.Id).Count == 0).ToArray()));
    }
}
