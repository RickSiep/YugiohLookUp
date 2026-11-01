using System.Security.Cryptography;
using System.Text;
using YugiohLookUp.Core.Application;
using YugiohLookUp.Core.Domain;

namespace YugiohLookUp.Import;

public sealed record PackTextResult(CatalogDocument Document, IReadOnlyList<Diagnostic> Diagnostics);

public static class PackTextReader
{
    public static PackTextResult Parse(string text, string source)
    {
        var cards = new Dictionary<string, Card>(StringComparer.Ordinal);
        var packs = new Dictionary<string, Pack>(StringComparer.Ordinal);
        var links = new Dictionary<(string, string), int>();
        var diagnostics = new List<Diagnostic>();
        Pack? current = null;
        var headingLine = 0;
        var sectionCards = 0;
        var lineNumber = 0;
        using var reader = new StringReader(text);
        while (reader.ReadLine() is { } original)
        {
            lineNumber++;
            var line = original.Trim();
            if (line.Length == 0)
                continue;
            if (line.StartsWith("Pack:", StringComparison.OrdinalIgnoreCase))
            {
                CheckEmptySection();
                headingLine = lineNumber;
                sectionCards = 0;
                var name = line[5..].Trim();
                current = null;
                if (name.Length == 0)
                {
                    Error("pack-heading", lineNumber, "Pack heading requires a name after 'Pack:'.");
                    continue;
                }
                var id = StableId("pack", name);
                if (packs.ContainsKey(id))
                {
                    Error("duplicate-pack", lineNumber, $"Pack '{name}' is declared more than once; combine its sections.");
                    current = packs[id];
                    continue;
                }
                current = new Pack { Id = id, Name = name, ImagePath = $"assets/packs/{id}.webp" };
                packs.Add(id, current);
                continue;
            }
            if (current is null)
            {
                Error("card-without-pack", lineNumber, $"Card '{line}' must follow a valid 'Pack: Name' heading.");
                continue;
            }
            sectionCards++;
            var cardId = StableId("card", line);
            if (!cards.ContainsKey(cardId))
                cards.Add(cardId, new Card { Id = cardId, Name = line, NormalizedName = NameNormalizer.Normalize(line) });
            var pair = (cardId, current.Id);
            if (links.TryGetValue(pair, out var firstLine))
                diagnostics.Add(new(DiagnosticSeverity.Warning, "duplicate-relationship", $"line {lineNumber}",
                    $"'{line}' already occurs in '{current.Name}' at line {firstLine}; keeping one relationship."));
            else
                links.Add(pair, lineNumber);
        }
        CheckEmptySection();
        var checksum = Hash(text);
        return new(new CatalogDocument
        {
            SchemaVersion = 1,
            DatasetVersion = $"text-{checksum}",
            Source = $"{source.Trim()}; formatted text SHA-256: {checksum}.",
            IsDemo = false,
            Cards = cards.Values.ToArray(),
            Packs = packs.Values.ToArray(),
            CardPacks = links.Keys.Select(pair => new CardPack { CardId = pair.Item1, PackId = pair.Item2 }).ToArray()
        }, diagnostics.AsReadOnly());

        void Error(string code, int number, string message) =>
            diagnostics.Add(new(DiagnosticSeverity.Error, code, $"line {number}", message));

        void CheckEmptySection()
        {
            if (current is not null && sectionCards == 0)
                Error("empty-pack", headingLine, $"Pack '{current.Name}' has no card entries.");
        }
    }

    private static string StableId(string kind, string name) =>
        $"{kind}-{Hash(name.Normalize(NormalizationForm.FormC).ToUpperInvariant())}";

    private static string Hash(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
