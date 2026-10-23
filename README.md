# Matteo Duel

A local Yu-Gi-Oh! practice client based on [MDPro3](https://github.com/daominah/mdpro3fork), with TCG Advanced and Genesys rules, AI opponents, a deck editor, and the original duel presentation.

## Playing

- **My Decks** opens your deck library directly. Each deck has its own **Format** button in the editor; TCG or Genesys and its point cap are saved inside the YDK file.
- **Play Solo** uses the selected deck's saved format. TCG and Genesys use separate AI deck libraries. Opponents incompatible with the current format or point cap are excluded, including from Random selection.
- The TCG library contains 57 builds with forbidden cards removed and limited copies corrected. Genesys has 52 compatible builds, with 10 fitting a 100-point cap. Custom AI decks are validated before a duel starts.
- The home screen prioritizes Solo and Decks. LP notifications distinguish damage from activation costs. Optional card-preview visuals cannot interrupt a monster's movement from hand to field.

## Build and verification

See [FORMAT-BUILD.md](FORMAT-BUILD.md) for Unity, native engine, runtime asset, data snapshot, and Windows build instructions. Runtime art/font bundles must be restored separately as described there; this repository does not provide a newly packaged game download.

```powershell
python Tools/prepare-solo-decks.py
python Tests/NativeServerProtocolTests.py --all-solo
```

The Solo audit checks every generated deck against the installed TCG/Genesys data. Native TCP checks cover all 109 builds and 50 below-cap rejection cases. Packaged runtime checks cover the opponent catalog, actual AI admission, deck profiles, direct-damage mapping, hand-to-field movement, and field destruction without removing unrelated hand cards.

These checks establish deck construction and tested gameplay paths; every card interaction and every AI strategy's playing strength have not been exhaustively tested. Rule snapshots and unavailable card names are recorded in Data/format-data-sources.json; regenerate and revalidate the Solo libraries when updating rules or card data.

## Credits and license

Based on the MDPro3 source mirror at commit 025d2b8. Upstream history, source attribution, and third-party notices are retained. See [LICENSE](LICENSE). Konami/Shueisha artwork and audio remain the property of their respective owners. This fan modification is not affiliated with or endorsed by Konami.
