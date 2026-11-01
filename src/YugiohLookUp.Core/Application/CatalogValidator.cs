using YugiohLookUp.Core.Domain;

namespace YugiohLookUp.Core.Application;

public enum DiagnosticSeverity { Warning, Error }

public sealed record Diagnostic(DiagnosticSeverity Severity, string Code, string Location, string Message);

public static class CatalogValidator
{
    public static IReadOnlyList<Diagnostic> Validate(CatalogDocument document)
    {
        var diagnostics = new List<Diagnostic>();
        void Error(string code, string location, string message) =>
            diagnostics.Add(new(DiagnosticSeverity.Error, code, location, message));

        if (document.SchemaVersion != 1)
            Error("schema", "catalog", "Only schemaVersion 1 is supported.");
        if (string.IsNullOrWhiteSpace(document.DatasetVersion) || string.IsNullOrWhiteSpace(document.Source))
            Error("metadata", "catalog", "Dataset version and source/provenance are required.");
        if (document.Cards is null || document.Packs is null || document.CardPacks is null)
        {
            Error("collections", "catalog", "Cards, packs, and cardPacks must be arrays, not null.");
            return diagnostics;
        }
        if (document.Cards.Count == 0 || document.Packs.Count == 0)
            Error("empty", "catalog", "At least one card and one pack are required.");

        var cardIds = new HashSet<string>(StringComparer.Ordinal);
        var packIds = new HashSet<string>(StringComparer.Ordinal);
        var names = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        for (var i = 0; i < document.Cards.Count; i++)
        {
            var card = document.Cards[i];
            var location = $"cards[{i}]";
            if (card is null)
            {
                Error("card", location, "Card cannot be null.");
                continue;
            }
            if (string.IsNullOrWhiteSpace(card.Id) || string.IsNullOrWhiteSpace(card.Name))
                Error("card", location, "Card ID and official name are required.");
            if (!string.IsNullOrWhiteSpace(card.Id) && !cardIds.Add(card.Id))
                Error("duplicate-card", location, $"Duplicate card ID '{card.Id}'.");
            if (!string.IsNullOrWhiteSpace(card.Name))
            {
                var normalized = NameNormalizer.Normalize(card.Name);
                if (normalized.Length == 0 || card.NormalizedName != normalized)
                    Error("normalized-name", location, $"Normalized name must equal '{normalized}'.");
                TrackName(normalized, card.Id);
            }
            if (card.Aliases is null)
                Error("aliases", location, "Aliases must be an array, not null.");
            else
                foreach (var alias in card.Aliases)
                {
                    if (string.IsNullOrWhiteSpace(alias) || NameNormalizer.Normalize(alias).Length == 0)
                        Error("alias", location, "Aliases must contain a searchable name.");
                    else
                        TrackName(NameNormalizer.Normalize(alias), card.Id);
                }
            if (card.ImagePath is not null && !IsAssetPath(card.ImagePath))
                Error("asset-path", location, "Card artwork must use a safe base-relative asset URL.");
        }
        for (var i = 0; i < document.Packs.Count; i++)
        {
            var pack = document.Packs[i];
            var location = $"packs[{i}]";
            if (pack is null)
            {
                Error("pack", location, "Pack cannot be null.");
                continue;
            }
            if (string.IsNullOrWhiteSpace(pack.Id) || string.IsNullOrWhiteSpace(pack.Name))
                Error("pack", location, "Pack ID and in-game name are required.");
            if (!string.IsNullOrWhiteSpace(pack.Id) && !packIds.Add(pack.Id))
                Error("duplicate-pack", location, $"Duplicate pack ID '{pack.Id}'.");
            if (!IsAssetPath(pack.ImagePath))
                Error("asset-path", location, "Pack artwork must use a safe base-relative asset URL.");
        }
        var relationships = new HashSet<(string, string)>();
        var linkedCardIds = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < document.CardPacks.Count; i++)
        {
            var link = document.CardPacks[i];
            var location = $"cardPacks[{i}]";
            if (link is null || !cardIds.Contains(link.CardId) || !packIds.Contains(link.PackId))
                Error("relationship", location, "Relationship references a missing card or pack.");
            else if (!relationships.Add((link.CardId, link.PackId)))
                Error("duplicate-relationship", location, $"Duplicate relationship '{link.CardId}' / '{link.PackId}'.");
            else
                linkedCardIds.Add(link.CardId);
        }
        foreach (var (name, ids) in names.Where(pair => pair.Value.Count > 1).OrderBy(pair => pair.Key, StringComparer.Ordinal))
            diagnostics.Add(new(DiagnosticSeverity.Warning, "name-collision", name,
                $"Ambiguous normalized name/alias shared by: {string.Join(", ", ids.Order(StringComparer.Ordinal))}."));
        foreach (var card in document.Cards.Where(card => card is not null && !string.IsNullOrWhiteSpace(card.Id)))
            if (!linkedCardIds.Contains(card.Id))
                diagnostics.Add(new(DiagnosticSeverity.Warning, "no-pack", card.Id, "Card has no known pack."));
        return diagnostics;

        void TrackName(string name, string id)
        {
            if (!names.TryGetValue(name, out var ids))
                names[name] = ids = new(StringComparer.Ordinal);
            ids.Add(id);
        }
    }

    public static bool IsAssetPath(string? path) =>
        !string.IsNullOrWhiteSpace(path) &&
        path.StartsWith("assets/", StringComparison.Ordinal) &&
        !path.Contains('\\') && !path.Contains('%') && !path.Contains(':') &&
        !path.Contains('?') && !path.Contains('#') &&
        !path.Any(char.IsControl) &&
        path.Split('/').All(segment => segment.Length > 0 && segment is not "." and not "..");
}
