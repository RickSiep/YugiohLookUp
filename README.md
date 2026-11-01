# ARC-V Tag Force Special Pack Finder

A .NET 10 standalone Blazor WebAssembly PWA for local card-to-pack searches, deck-list lookup, and pack browsing. No backend, database, external card API, MAUI, or UI component framework.

**Current status: user-supplied catalog and pack artwork imported.** The app bundles 7,089 cards, 182 packs, and 9,534 relationships from the supplied English Wikipedia-derived text list. The user reports full game coverage; this has not been independently reconciled with the original source. Pack images are assigned sequentially from `pack_tex01.png` in catalog entry order; visual mappings remain unverified.

## Run

Install a current serviced .NET 10 SDK compatible with `global.json`. From the project root:

```powershell
dotnet restore YugiohLookUp.slnx
dotnet run --project src\YugiohLookUp.Web
```

Open the URL printed by the development server. Search `Dark Magician` or `Shaddoll Beast`.

Example bulk input:

```text
Dark Magician x2
Dark Magician
Shaddoll Beast 2
2x Shaddoll Dragon
Missing Card
```

Ambiguous matches require explicit candidate selection. Repeated matched cards, including aliases if supplied, merge by summing quantities. Packs are ranked by distinct requested cards, with requested-copy counts shown separately. These are coverage counts, not guaranteed drops or purchase recommendations.

Supported quantity forms: `Name x2`, `Name X2`, `Name x 2`, `Name 2`, `2 Name`, `2x Name`, and `2X Name`. Quantities must be positive 32-bit integers; missing quantities default to one. Exact/normalized/alias whole-line matches take precedence to protect numeric names. Empty lines are ignored; invalid, unknown, and ambiguous lines retain their original line numbers.

Matching uses case-insensitive official names, conservative normalization, then curated aliases. Normalization applies browser-supported Unicode canonical normalization, full-width ASCII folding, invariant casing, collapsed whitespace, hyphens as spaces, and removal of apostrophes, quotes, periods, commas, colons, semicolons, exclamation/question marks. Other symbols and digits remain meaningful. Different cards that collide are not silently merged. There is no fuzzy matching.

## Structure

| Project | Responsibility |
| --- | --- |
| `src\YugiohLookUp.Core` | Entities, validation, indexed lookup, parsing, grouping; no UI/browser/filesystem dependencies |
| `src\YugiohLookUp.Web` | Shared responsive dark UI, asynchronous catalog loading, local assets, PWA caching |
| `tools\YugiohLookUp.Import` | Pack-heading text and canonical JSON import, filesystem checks, diagnostics, deterministic output |
| `tests\YugiohLookUp.Tests` | xUnit business-rule and importer tests |

The catalog is loaded once per browser application instance. Core lookup and grouping are independently testable. Required pack images are precached but rendered lazily; optional card images are not bundled by default. See [data import instructions](data/README.md).

## Checks

```powershell
dotnet test tests\YugiohLookUp.Tests
dotnet build YugiohLookUp.slnx -c Release
dotnet format YugiohLookUp.slnx --verify-no-changes
dotnet publish src\YugiohLookUp.Web -c Release -o artifacts\publish
```

Uses the stable template-provided xUnit 2/VSTest stack. Coverage tooling is not required. Browser/device acceptance is separate from unit tests.

## Publish and offline use

Deploy **only** `artifacts\publish\wwwroot` to HTTPS static hosting. Configure SPA navigation fallback to `index.html` for extensionless routes, but return real 404s for missing static assets. Serve JavaScript, JSON, WebP/JPEG/SVG, web manifests, and WebAssembly with appropriate MIME types. Enable compression; when serving `.br`/`.gz` variants, set the matching content encoding and original resource MIME type.

The app currently targets the host root (`<base href="/" />`). For a deployment subdirectory, change the base href in source `wwwroot\index.html` before publishing and configure the hosting fallback under that path. The worker derives its base from its own URL, with caches scoped to that path. Do not edit deployed assets after publishing: the manifest hashes must match their contents.

Use consistent/atomic deployments. Do not cache the service worker/asset manifest indefinitely at the host/CDN. First installation requires an internet connection. On Android Chrome, visit the HTTPS site and choose the browser's install/Add to Home screen option. Desktop browsers can use the site without installation.

**Wait for “Offline cache ready” before disconnecting.** Readiness checks the controlling worker's actual cache, not the connectivity indicator. Production caches contain the app, bundled JSON, all pack artwork, icons, and local styles. Development builds explicitly do not support offline caching.

Updates download into a separate integrity-checked snapshot. A failed download leaves the old snapshot intact. A completed update waits until all app tabs/windows close; the UI tells you to reopen. Unsaved bulk text is not force-reloaded. Missing cache entries are reported and can be recached through the retry button while online. A failed repair does not discard remaining cached entries.

Browser storage can be cleared or evicted; offline installation is not a permanent native-storage guarantee. A hard/forced browser reload may bypass the service worker and requires a connection; normal refresh and relaunch are supported offline.

## Outstanding release prerequisites

- Obtain the exact Wikipedia source page/revision and reuse attribution for the supplied list.
- Reconcile the imported ARC-V Tag Force Special catalog against that source, including translation/version details and multiple-pack relationships.
- Confirm pack artwork reuse permission, optimize and visually verify every sequential pack mapping.
- Select HTTPS hosting and verify actual Android installation/offline use and modest-device performance.

Saved decks, CSV deck import, fuzzy correction, pack optimization, and downloadable card-art collections are intentionally not implemented. See [acceptance status](docs/ACCEPTANCE.md).
