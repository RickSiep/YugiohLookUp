# Catalog import and asset preparation

There is no runtime dependency on Scribd or any other data website. The bundled catalog now comes from the user's formatted English Wikipedia-derived text file. The synthetic JSON fixture remains available for development/testing.

Source reference supplied by the user:
https://www.scribd.com/document/664782871/Yu-Gi-Oh-ARC-V-Tag-Force-Special-Card-List-PSP-By-RemixV4-GameFAQs

The user supplied `formatted-cards.txt` and reports full English game coverage. Exact Wikipedia page/revision, reuse attribution, and independently reconciled completeness are still pending. User-supplied pack artwork is bundled with provisional sequential mappings. Record source/provenance and artwork reuse permission before redistributing the catalog. Raw local exports are kept in ignored `data\local`; do not publish originals.

## Pack-heading text input

UTF-8 `.txt` files can be imported directly:

```text
Pack: Pack Name
Card Name
Another Card

Pack: Other Pack Name
Card Name
```

Blank lines are ignored; each nonblank line following a heading is a card name, not a quantity. Headings are case-insensitive and require nonempty names. Empty or repeated pack sections and cards before a heading fail with source line diagnostics.

Card identities merge only by case-insensitive, Unicode canonical-equivalent names, not punctuation-normalized similarity. A repeated card across packs retains all relationships. Repeated entries within one pack generate a warning with both line numbers and keep one relationship. Differently punctuated names remain distinct and any search collisions are reported.

Stable IDs are SHA-256 hashes of canonically normalized, invariant-uppercase names, prefixed `card-`/`pack-`. IDs do not change with input order, line endings, or unrelated additions, but renaming changes identity; use canonical JSON and preserve explicit IDs if making name corrections after adoption. Text imports assign `assets/packs/<pack-id>.webp` image URLs, so adding files at those paths requires no logic changes. To use other names/formats or add aliases, edit the canonical JSON and import that instead.

Text input requires `--source` to record provenance. The importer adds the input text's SHA-256 to the output metadata and dataset version. It does not infer rights or completeness from successful validation.

## Canonical input

The importer accepts a local UTF-8 JSON object with camel-case keys:

```json
{
  "schemaVersion": 1,
  "datasetVersion": "local-1",
  "source": "Record source identity/checksum, permission, language/game version, and coverage notes here.",
  "isDemo": false,
  "cards": [
    { "id": "card-example", "name": "Example Card", "aliases": [] }
  ],
  "packs": [
    { "id": "pack-example", "name": "Example Pack", "imagePath": "assets/packs/pack-example.webp" }
  ],
  "cardPacks": [
    { "cardId": "card-example", "packId": "pack-example" }
  ]
}
```

These example names are illustrative, not actual game relationships.

- IDs are unique stable strings, not row numbers or indexes that change when input order changes. Preserve trustworthy source IDs or maintain explicit canonical IDs across imports.
- `normalizedName` is computed by the importer; input can omit it. Official names and pack display spelling are preserved.
- Card `imagePath` and pack `description` are optional. Aliases default to an empty array. The dataset must include nonempty cards/packs collections, metadata, and valid relationship endpoints.
- A card may belong to any number of packs; never discard additional relationships or silently merge duplicate IDs.
- Asset URLs must begin with `assets/`, use URL `/` separators, and contain no `..`, escaped segments, query strings, fragments, absolute URLs, or filesystem paths.
- Put optimized images beneath `src\YugiohLookUp.Web\wwwroot\assets\packs`. Prefer WebP/JPEG, preserve aspect ratio, and choose dimensions/quality based on mobile readability and measured size. Replace images or paths without changing application code.

## Commands

Import the prepared working copy supplied for this project:

```powershell
dotnet run --project tools\YugiohLookUp.Import -- data\local\formatted-cards.txt src\YugiohLookUp.Web\wwwroot\data\catalog.json src\YugiohLookUp.Web\wwwroot --source "User-supplied Wikipedia-derived list; English ARC-V Tag Force Special; user reports full coverage, not independently verified"
```

For other correctly formatted files, replace the first path, for example `"D:\Downloads\firefox\formatted-cards.txt"`. The supplied original had nine incorrect `Pack:` headings and must use the corrected working copy until those headings are fixed at the source. Draft imports allow missing artwork; `--strict` requires actual referenced files.

Regenerate the demo bundle:

```powershell
dotnet run --project tools\YugiohLookUp.Import -- data\fixtures\catalog.json src\YugiohLookUp.Web\wwwroot\data\catalog.json src\YugiohLookUp.Web\wwwroot
```

Import authorized canonical data with release asset checks:

```powershell
dotnet run --project tools\YugiohLookUp.Import -- data\local\catalog.json src\YugiohLookUp.Web\wwwroot\data\catalog.json src\YugiohLookUp.Web\wwwroot --strict
```

Exit codes: `0` output written, `1` invalid data/I/O failure, `2` invalid command usage. Validation errors do not overwrite previous output. Successful output is sorted by stable IDs/relationships and normalized deterministically; identical input records produce identical UTF-8 output regardless of ordering.

Diagnostics include severity, code, record index/ID, and explanation. Draft mode reports missing images as warnings. Strict mode rejects missing referenced images, placeholder pack artwork, and datasets marked `isDemo`. Duplicate IDs/relationships, invalid paths/endpoints, empty required names, or unsupported schemas fail either mode. Name/alias collisions and cards without packs are reported for review; the UI keeps ambiguity/incomplete data visible. Strict validation checks image references/existence, not image contents or permission; visual correctness and rights must be reviewed separately.

The importer parses the documented pack-heading format, **not** arbitrary Scribd/PDF/OCR text. It cannot distinguish a card incorrectly labelled as a heading from a genuine pack, or detect prose mistakenly included as card lines. Inspect formatting and reconcile the source before release.

## Current import record

- Original file: `D:\Downloads\firefox\formatted-cards.txt`, unchanged.
- Local backup: `data\local\formatted-cards.original.txt`; corrected working copy: `data\local\formatted-cards.txt`.
- Original file SHA-256: `da3ab79f1ad96af7282ec6d35b1b863532ac1d38e6bf42aa97408459c9826aeb`.
- With user approval, headings `T.A.D.P.O.L.E.`, `NEX`, `ZERO-MAX`, `TGX1-HL`, `TGX300`, `TGX3-DX2`, `TG-SX1`, `TG1-EM1`, and `Z-ONE` became card entries in the preceding pack. No other names/mappings were manually changed.
- Result: 7,089 unique card names, 182 packs, 9,534 unique relationships. The importer reported 52 duplicate relationships and 182 missing image references; no normalized-name collisions.
- Import diagnostics: `data\local\import-report.txt`. The working JSON copy is `data\local\catalog.json`; deployed data is `src\YugiohLookUp.Web\wwwroot\data\catalog.json`.
- Pack artwork was copied from `D:\code\Yugiohtxtformatter\gameOutput\files\USRDIR\shop\1` to `src\YugiohLookUp.Web\wwwroot\assets\packs`. Both catalogs map their existing pack entry order to `pack_tex01.png` through `pack_tex182.png`; `pack_tex00.png` was ignored and `pack_tex183.png` was copied but remains unassigned. These mappings are user-approved assumptions, not visually verified. Reimport canonical JSON to preserve manual image corrections; importing text again resets image paths.
- All entries in the formatted file are accounted for after the explicit heading corrections and reported duplicate removal. Source-level accuracy/completeness remains unverified.

## Completeness checklist before release

1. Record source identity/checksum and permission, game/version/language, and conversion procedure.
2. Account for every pack section, unparsed line, and duplicate/ambiguous name; reconcile known pack/card/relationship totals.
3. Spot-check representative names, numeric names, aliases, and cards present in multiple packs against the source/game.
4. Verify every pack image shows the corresponding in-game artwork and matches the pack's translated display name.
5. Run strict validation and tests, publish, and confirm offline searches/browsing include initially unvisited packs.

Fixtures/placeholders cannot satisfy full-data or actual-artwork acceptance.
