# MVP acceptance status

The app now bundles the user's English Wikipedia-derived formatted list: 7,089 cards, 182 packs, and 9,534 relationships. The user reports full coverage; independent source reconciliation and genuine pack artwork remain pending. Browser checks use published Release output in headless desktop Edge and a local static server. Helpers are stopped/removed after checking. No physical Android or public HTTPS-hosted acceptance has been performed.

| # | Requested criterion | Status |
| --- | --- | --- |
| 1 | Install/open on Android | Pending physical Android/HTTPS host verification |
| 2 | Desktop browser access | Verified in published desktop Edge |
| 3 | Offline after complete caching | Verified normal refresh, previously unvisited pack details/artwork, and new-tab launch with server unavailable; Android pending |
| 4 | Full imported dataset local | User's full-list text imported locally; exact source/revision and independent completeness/accuracy reconciliation pending |
| 5 | Individual card search | Verified with imported Dark Magician and full-width input; aliases covered by fixtures |
| 6 | All containing packs displayed | Imported Dark Magician shows ten containing packs; independent source accuracy pending |
| 7 | In-game pack artwork | Blocked: genuine artwork unavailable; imported packs use local fallbacks |
| 8 | Paste complete deck list | Verified real Enter-key multiline input using imported names |
| 9 | Names/quantities parsed correctly | Automated coverage of formats, blanks, numeric names, malformed input, merging, and overflow; displayed per-card quantities verified in published browser |
| 10 | Cards grouped by pack | Automated and published-browser verification of all-pack grouping and distinct/copy counts |
| 11 | Unmatched cards identified | Verified; ambiguity requires explicit choice |
| 12 | Missing images/incomplete data do not crash | Missing artwork uses a loaded local fallback and notice; corrupt JSON surfaces an error and retries successfully; both verified in browser. Unknown relationships diagnosed |
| 13 | Core automated tests | 60 passing xUnit cases covering lookup, parsing/grouping, validation, text imports, and deterministic/failing imports |
| 14 | No external API/backend | All app resources are local static assets; core searches require no network |

Additional published checks: a failed snapshot update retains the previous cache; completed updates wait for all app windows to close, then activate and remove the previous snapshot. Missing cache entries are detected, failed repairs preserve remaining entries, and online retries restore readiness. A 390-pixel mobile-emulated layout showed no horizontal overflow during bulk results. No unhandled browser exceptions occurred during these scenarios. Mobile emulation does not establish physical Android acceptance.

After the user-data import, published Edge checks verified full-width card search, Dark Magician's multiple-pack associations, missing artwork fallback, Enter-separated bulk names with duplicate quantity merging and unmatched source line numbers, and offline reload/browsing of all 182 packs. Unicode startup exposed WebAssembly's unsupported FormKC normalization; the shared normalizer now uses FormC with explicit full-width ASCII folding. Browser checks also caught and fixed per-card quantities being rendered as literal Razor text.

Initial fixture Release output measured approximately 16 MB including the framework and compressed/uncompressed hosting variants. This is not a network-transfer size or the final artwork budget. Real dataset/artwork size, startup/lookup timings on a modest Android device, touch/install behavior, and hosting/subdirectory behavior remain release checks.

Before declaring the MVP complete, reconcile the imported dataset with the exact source and reuse attribution, verify real pack artwork, run strict import validation, and perform Android installation/offline and performance checks on the chosen HTTPS deployment.
