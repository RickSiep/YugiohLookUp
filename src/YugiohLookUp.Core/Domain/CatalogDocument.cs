using System.Text.Json.Serialization;

namespace YugiohLookUp.Core.Domain;

public sealed record Card
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string NormalizedName { get; init; } = "";
    public string? ImagePath { get; init; }
    public IReadOnlyList<string> Aliases { get; init; } = [];
}

public sealed record Pack
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string ImagePath { get; init; } = "";
    public string? Description { get; init; }
}

public sealed record CardPack
{
    public string CardId { get; init; } = "";
    public string PackId { get; init; } = "";
}

public sealed record CatalogDocument
{
    public int SchemaVersion { get; init; }
    public string DatasetVersion { get; init; } = "";
    public string Source { get; init; } = "";
    public bool IsDemo { get; init; }
    public IReadOnlyList<Card> Cards { get; init; } = [];
    public IReadOnlyList<Pack> Packs { get; init; } = [];
    public IReadOnlyList<CardPack> CardPacks { get; init; } = [];
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = true)]
[JsonSerializable(typeof(CatalogDocument))]
public partial class CatalogJsonContext : JsonSerializerContext;
