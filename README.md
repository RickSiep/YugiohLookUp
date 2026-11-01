# ARC-V Tag Force Special Pack Finder

A card and pack lookup app for **Yu-Gi-Oh! ARC-V Tag Force Special**. Find which packs contain the cards you need, look up a whole deck list, or browse packs and their contents.

The app includes 7,089 cards and 182 packs with pack artwork. It runs entirely in your browser, with no account or backend required.

## What you can do

- **Search for a card** to see the packs it appears in.
- **Paste a deck list** to find packs containing your requested cards. Results are ranked by how many different cards from your list each pack contains.
- **Browse packs** and explore their card lists.
- **Use it offline** once the published app has cached its files.

Pack results show which cards are available, not guaranteed pulls or drop rates.

## Looking up a deck

Enter one card per line, optionally with a quantity:

```text
Dark Magician x2
Shaddoll Beast 2
2x Shaddoll Dragon
```

Names are matched without regard to capitalization and common punctuation differences. Repeated cards are combined, and any unrecognized or ambiguous names are highlighted so you can review them.

## Offline use

Open the published site while connected to the internet and wait for **"Offline cache ready"** before disconnecting. You can then search cards and browse the bundled packs and artwork offline.

Supported browsers also let you install the app using their **Install** or **Add to Home screen** option. When an update is ready, close all app tabs or windows and reopen it to use the new version.

Offline caching is available in published builds, not the development server. Clearing browser storage removes the offline files.

## Run locally

The app is built with .NET 10 and Blazor WebAssembly. Install a .NET 10 SDK compatible with `global.json`, then run these commands from the project folder:

```powershell
dotnet restore YugiohLookUp.slnx
dotnet run --project src\YugiohLookUp.Web
```

Open the address printed in the terminal.

To create a production build:

```powershell
dotnet publish src\YugiohLookUp.Web -c Release -o artifacts\publish
```

Deploy the contents of `artifacts\publish\wwwroot` to HTTPS static hosting. For a GitHub Pages project site, set the base URL in `src\YugiohLookUp.Web\wwwroot\index.html` to `/<repository-name>/` before publishing.

## About the catalog

The bundled catalog comes from a supplied English Wikipedia-derived card list. Complete game coverage and every pack artwork mapping have not been independently verified.

For development details, see the [data import instructions](data/README.md) and [acceptance status](docs/ACCEPTANCE.md).
