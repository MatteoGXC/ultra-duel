# Matteo Duel

A local Yu-Gi-Oh! practice client based on [MDPro3](https://github.com/daominah/mdpro3fork), with TCG Advanced and Genesys rules, AI opponents, a deck editor, and the original duel presentation.

## Playing

- **My Decks** opens your deck library directly. Corner badges show each deck's saved format: TCG, Genesys (100 points), Genesys 0, or its custom point cap. Each deck has its own **Format** button in the editor; the format and cap are saved inside the YDK file.
- **Play Solo** starts with the selected deck's saved format and has its own format/point-cap switch and player-deck selector. TCG and Genesys use separate AI deck libraries. Opponents incompatible with the current format or point cap are excluded, including from Random selection.
- Solo offers 14 TCG and 15 Genesys published lists, with a mix of Casual, Standard, and Hard practice opponents. Difficulty labels are estimates, not tournament rankings. Sources and exact card arrays are recorded in `Data/solo-published-decks.json`. Cards are imported unchanged; there is no automatic rebuilding, substitution, trimming, or staple filling. Custom AI decks are validated before play.
- The home screen prioritizes Solo and Decks. LP notifications distinguish damage from activation costs. Optional card-preview visuals cannot interrupt a monster's movement from hand to field.

## Build and verification

See [FORMAT-BUILD.md](FORMAT-BUILD.md) for Unity, native engine, runtime asset, data snapshot, and Windows build instructions. Runtime art/font bundles must be restored separately as described there; this repository does not provide a newly packaged game download.

```powershell
python Tools/prepare-solo-decks.py
python Tests/NativeServerProtocolTests.py --all-solo
```

The Solo audit validates every published list against the installed TCG/Genesys snapshots and records file hashes. Native TCP checks cover every catalog entry and below-cap rejection for every Genesys list. Packaged runtime checks cover the opponent catalog, AI admission, deck profiles, damage mapping, hand movement, and destruction.

Only sourced, compatible opponents are offered, including by Random. An archetype without a compatible published list is unavailable; Pendulum/Link strategies cannot be converted to Genesys unchanged. Lower custom caps may leave no built-in opponents; the Custom AI option still accepts legal user decks. These checks establish deck legality and tested paths, not exhaustive combo execution or playing strength. To update rules, revalidate the published snapshots; never alter a source list to force it to fit.

## Credits and license

Based on the MDPro3 source mirror at commit 025d2b8. Upstream history, source attribution, and third-party notices are retained. See [LICENSE](LICENSE). Konami/Shueisha artwork and audio remain the property of their respective owners. This fan modification is not affiliated with or endorsed by Konami.
