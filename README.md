# Ultra Duel

**A free, open-source Yu-Gi-Oh! practice and multiplayer client for Windows and macOS.**

Ultra Duel is an independent community-driven project built upon [MDPro3](https://github.com/daominah/mdpro3fork). It expands the original client with TCG Advanced and Genesys formats, AI opponents, online multiplayer, deck management, progression systems, and an updated interface.

Whether you're testing a competitive deck, learning new strategies, or dueling friends, Ultra Duel provides a flexible environment for practicing Yu-Gi-Oh!

**Free to play • Open source • TCG & Genesys • Solo & Multiplayer**

---

## Download Ultra Duel

Choose the version compatible with your operating system.

| Platform | Download |
|---|---|
| **Windows** | [Download for Windows](https://github.com/MatteoGXC/ultra-duel/releases/latest/download/Ultra-Duel-Setup.exe) |
| **macOS — Intel** | [View Intel Mac releases](https://github.com/MatteoGXC/ultra-duel/releases/tag/macos-v0.1.12) |
| **macOS — Apple Silicon** | [View Apple Silicon releases](https://github.com/MatteoGXC/ultra-duel/releases/tag/macos-v0.1.10) |

For all available versions, visit the [Downloads Guide](DOWNLOADS.md) or browse [GitHub Releases](https://github.com/MatteoGXC/ultra-duel/releases).

### Windows installation

1. Download `Ultra-Duel-Setup.exe`.
2. Run the installer and select an installation directory.
3. Allow the installer to download and verify the required game files.
4. Launch **Ultra Duel** using the desktop shortcut.

Unity does not need to be installed to play.

**Already have Ultra Duel or Matteo Duel installed?**

Download the [Ultra Duel Updater](https://github.com/MatteoGXC/ultra-duel/releases/latest/download/Ultra-Duel-Updater.zip), extract it into your installation directory, close the game, and run `Update-Ultra-Duel.cmd`.

The updater downloads changed files while preserving your decks, settings, and local progress.

> **Important:** Keep the executable and accompanying data, runtime, and asset folders together. The executable alone is not sufficient to run the game. The internal `Matteo-Duel_Data` directory is retained for compatibility with existing installations.

---

## Features

### TCG Advanced & Genesys

Ultra Duel supports multiple Yu-Gi-Oh! formats, allowing players to practice under different deck-building rules.

- **TCG Advanced:** Traditional competitive Yu-Gi-Oh! deck construction and gameplay.
- **Genesys:** Point-based deck building with support for 100-point, 0-point, and custom limits.
- **Per-deck format settings:** Each deck independently remembers its selected format and point cap.
- **Format-aware validation:** Decks and AI opponents are checked against the selected format.

Format settings are stored within each deck's YDK file.

### Deck editor

Build, customize, and manage your decks directly from the client.

- Browse your deck library without selecting a format first.
- Assign individual formats to your decks.
- View each deck's selected format from its library badge.
- Edit your Main, Extra, and Side Deck configurations.
- Save decks for use in Solo or Multiplayer.

### Solo practice

Practice against AI opponents without needing an internet connection after installation.

Ultra Duel includes:

- 14 published TCG AI deck lists.
- 15 published Genesys AI deck lists.
- Casual, Standard, and Hard difficulty labels.
- Random opponent selection.
- Custom AI deck support.
- Format and point-cap filtering.

Published deck lists are preserved without automatic substitutions or rebuilding. Difficulty labels are estimates rather than formal rankings.

Deck sources and card lists are documented in `Data/solo-published-decks.json`.

### Online multiplayer

Challenge friends using Ultra Duel's hosted multiplayer services.

1. Open **FRIENDS** and create an account.
2. Add another player using their username.
3. Accept the friend request.
4. Open **DUEL** and create or join a private room.
5. Select your format and begin dueling.

Multiplayer connections use Ultra Duel's hosted server infrastructure, eliminating the need for players to configure router port forwarding.

Both players should use compatible client versions.

Server deployment and maintenance documentation is available in [Server/README.md](Server/README.md).

### Home screen and progression

Ultra Duel includes a redesigned angular home interface with access to:

- **DUEL** — Online multiplayer.
- **DECK** — Deck editing and management.
- **SOLO** — AI practice.
- **SHOP** — Cosmetic customization.
- **FRIENDS** — Friend management.
- **MISSIONS** — In-game objectives.
- **PROFILE** — Player information and customization.
- **DUEL PASS** — Free progression rewards.

The free Duel Pass contains 20 tiers without an expiration date.

Each win awards 100 XP, with a new tier available every 200 XP. Rewards include cosmetic gems, titles, and profile frames.

Gems are used for home-profile customization. No paid gameplay purchases are included.

Local progression is saved in `Progress/profile.json` and preserved during supported updates.

### Card database and gameplay improvements

Ultra Duel builds upon MDPro3's duel client and includes additional card database and gameplay updates.

The v0.1.2 content update introduced:

- 300 additional card IDs.
- 204 corrected availability flags.
- Updated maintained card scripts.
- 466 additional artwork sources.
- English Burst Protocol indexing with 101 official database entries.

Card availability is maintained separately from TCG legality. Cards available in the OCG are not automatically treated as TCG legal.

Update sources and snapshot details are recorded in [Data/card-update-audit.json](Data/card-update-audit.json).

Additional gameplay improvements include corrected Life Point display handling, damage and activation-cost notifications, and card-movement fixes.

### Updates

Ultra Duel checks GitHub for available releases and development updates.

Players are notified when a new packaged version is available. Development commits are distinguished from public releases, and Windows release notifications direct players to the updater.

---

## Development and building from source

Ultra Duel is developed primarily using **Unity and C#**, with native C++ duel-engine components, Lua card-effect scripts, and additional development tools.

The repository includes source code, project configuration, development scripts, and automated verification tools.

### Requirements

- Unity Editor compatible with the project (Unity `6000.0.28f1` is documented in its build history).
- Appropriate C# and C++ development tooling.
- Python for supporting development scripts.
- Required runtime resources and dependencies described in the build documentation.

See [FORMAT-BUILD.md](FORMAT-BUILD.md) for complete instructions covering Unity builds, the native engine, runtime dependencies, assets, data snapshots, and Windows packaging.

Developers building from source must supply the required runtime art and font resources as described in the build documentation.

### Running verification

From the project directory:

```powershell
python Tools/prepare-solo-decks.py
python Tests/NativeServerProtocolTests.py --all-solo
```

Existing tests cover areas including:

- TCG and Genesys deck compatibility.
- Published AI deck validation.
- Genesys point-cap restrictions.
- Native multiplayer protocol behavior.
- Life Point mapping.
- Hand-to-field card movement.
- Card destruction and game-state consistency.

These tests verify documented behavior and selected gameplay paths. They do not guarantee complete rule coverage or flawless execution of every possible card interaction.

---

## Contributing

Contributions to Ultra Duel are welcome.

Developers interested in improving gameplay, fixing bugs, refining the interface, improving platform compatibility, or expanding testing are encouraged to participate.

To contribute:

1. Fork the repository.
2. Create a dedicated development branch.
3. Make and test your changes.
4. Open a pull request describing the changes and their purpose.

Before contributing, review the project's existing code structure and build documentation.

Please preserve applicable licensing and copyright notices. Do not submit proprietary game assets or third-party materials without appropriate redistribution rights.

Bug reports should include the client version, operating system, steps to reproduce, and relevant logs when available.

---

## Supporting Ultra Duel

Ultra Duel is a free, community-driven project.

Although the game is free, operating its online services requires ongoing infrastructure, including VPS hosting, bandwidth, storage, and server maintenance.

Voluntary donations can help cover these operational expenses and support continued development.

**Donations are optional and do not provide gameplay advantages, exclusive cards, or special competitive privileges.**

*Donation links may be added when community funding becomes available.*

---

## Credits and acknowledgments

Ultra Duel is derived from **MDPro3** and builds upon the work of its original developers and contributors.

The project acknowledges the people and open-source communities responsible for developing the underlying client, duel engine, and related technologies.

**Upstream project:** [MDPro3 source mirror](https://github.com/daominah/mdpro3fork)

**Original source baseline:** Commit `025d2b8`

The repository retains upstream development history, attribution, and applicable third-party notices.

We appreciate the work of the original MDPro3 contributors and the broader open-source Yu-Gi-Oh! simulator community.

---

## License

Ultra Duel is distributed under the **GNU General Public License v3.0 (GPL-3.0)**, subject to the terms and notices applicable to its incorporated components.

See [LICENSE](LICENSE) for details.

The GPL license applicable to the project's code does not independently grant redistribution rights to third-party artwork, music, audio, trademarks, or other proprietary materials.

### Disclaimer

Ultra Duel is an independent, unofficial fan project.

Yu-Gi-Oh! and related names, characters, card illustrations, artwork, and other intellectual property belong to their respective rights holders, including Konami and Shueisha where applicable.

Ultra Duel is not affiliated with, authorized by, sponsored by, or endorsed by Konami or its affiliates.

---

**[Download Ultra Duel](https://github.com/MatteoGXC/ultra-duel/releases) | [Browse Source Code](https://github.com/MatteoGXC/ultra-duel) | [Report an Issue](https://github.com/MatteoGXC/ultra-duel/issues)**
