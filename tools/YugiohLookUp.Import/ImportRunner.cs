using System.Text.Json;
using YugiohLookUp.Core.Application;
using YugiohLookUp.Core.Domain;

namespace YugiohLookUp.Import;

public static class ImportRunner
{
    public static async Task<int> RunAsync(string[] args, TextWriter output)
    {
        if (args.Length < 3)
        {
            await WriteUsage();
            return 2;
        }
        var strict = false;
        string? source = null;
        for (var i = 3; i < args.Length; i++)
        {
            if (args[i] == "--strict" && !strict)
                strict = true;
            else if (args[i] == "--source" && source is null && i + 1 < args.Length && !string.IsNullOrWhiteSpace(args[i + 1]))
                source = args[++i];
            else
            {
                await WriteUsage();
                return 2;
            }
        }
        var textInput = Path.GetExtension(args[0]).Equals(".txt", StringComparison.OrdinalIgnoreCase);
        if (textInput && string.IsNullOrWhiteSpace(source))
        {
            await output.WriteLineAsync("Error usage: text input requires --source with source/game/language details.");
            return 2;
        }
        try
        {
            CatalogDocument? document;
            var diagnostics = new List<Diagnostic>();
            if (textInput)
            {
                var parsed = PackTextReader.Parse(await File.ReadAllTextAsync(args[0]), source!);
                document = parsed.Document;
                diagnostics.AddRange(parsed.Diagnostics);
            }
            else
            {
                await using var input = File.OpenRead(args[0]);
                document = await JsonSerializer.DeserializeAsync(input, CatalogJsonContext.Default.CatalogDocument);
                if (source is not null && document is not null)
                    document = document with { Source = source };
            }
            if (document is null)
                throw new InvalidDataException("Input must be a catalog object, not null.");
            var normalized = Normalize(document);
            diagnostics.AddRange(CatalogValidator.Validate(normalized));
            if (!Directory.Exists(args[2]))
                diagnostics.Add(new(DiagnosticSeverity.Error, "asset-root", args[2], "Asset root directory does not exist."));
            if (strict && document.IsDemo)
                diagnostics.Add(new(DiagnosticSeverity.Error, "demo", "catalog", "A demo dataset cannot pass strict release validation."));
            if (document.Packs is not null)
                foreach (var pack in document.Packs.Where(pack => pack is not null))
                    CheckAsset(pack.ImagePath, pack.Id, required: true);
            if (document.Cards is not null)
                foreach (var card in document.Cards.Where(card => card is not null && card.ImagePath is not null))
                    CheckAsset(card.ImagePath!, card.Id, required: false);
            foreach (var diagnostic in diagnostics)
                await output.WriteLineAsync($"{diagnostic.Severity} {diagnostic.Code} [{diagnostic.Location}]: {diagnostic.Message}");
            if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
                return 1;

            normalized = normalized with
            {
                Cards = normalized.Cards.Select(card => card with
                {
                    Aliases = card.Aliases.Order(StringComparer.Ordinal).ToArray()
                }).OrderBy(card => card.Id, StringComparer.Ordinal).ToArray(),
                Packs = normalized.Packs.OrderBy(pack => pack.Id, StringComparer.Ordinal).ToArray(),
                CardPacks = normalized.CardPacks.OrderBy(link => link.CardId, StringComparer.Ordinal)
                    .ThenBy(link => link.PackId, StringComparer.Ordinal).ToArray()
            };
            var destination = Path.GetFullPath(args[1]);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            var temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                await File.WriteAllTextAsync(temporary,
                    JsonSerializer.Serialize(normalized, CatalogJsonContext.Default.CatalogDocument) + "\n");
                File.Move(temporary, destination, overwrite: true);
            }
            finally
            {
                if (File.Exists(temporary))
                    File.Delete(temporary);
            }
            await output.WriteLineAsync($"Wrote {normalized.Cards.Count} cards, {normalized.Packs.Count} packs, {normalized.CardPacks.Count} relationships to {destination}.");
            return 0;

            void CheckAsset(string path, string id, bool required)
            {
                if (!CatalogValidator.IsAssetPath(path))
                    return;
                var placeholder = path == "assets/placeholder.svg";
                var missing = !File.Exists(Path.Combine(Path.GetFullPath(args[2]), path.Replace('/', Path.DirectorySeparatorChar)));
                if (missing || (required && placeholder))
                    diagnostics.Add(new(strict ? DiagnosticSeverity.Error : DiagnosticSeverity.Warning,
                        "missing-asset", id, missing ? $"Local asset '{path}' does not exist." : "Placeholder is not in-game pack artwork."));
            }
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or UnauthorizedAccessException or JsonException or ArgumentException)
        {
            await output.WriteLineAsync($"Error import: {exception.Message}");
            return 1;
        }

        Task WriteUsage() => output.WriteLineAsync(
            "Usage: YugiohLookUp.Import <input.json|input.txt> <output.json> <asset-root> [--strict] [--source \"source details\"]");
    }

    private static CatalogDocument Normalize(CatalogDocument document)
    {
        if (document.Cards is null || document.Packs is null || document.CardPacks is null ||
            document.Cards.Any(card => card is null || card.Name is null || card.Aliases is null) ||
            document.Packs.Any(pack => pack is null) || document.CardPacks.Any(link => link is null))
            return document;
        return document with
        {
            Cards = document.Cards.Select(card => card with
            {
                NormalizedName = NameNormalizer.Normalize(card.Name)
            }).ToArray()
        };
    }
}
