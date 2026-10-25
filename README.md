# Ultra Duel

**A free, open-source Yu-Gi-Oh! simulator for Windows and macOS.**

Ultra Duel is a community-driven Yu-Gi-Oh! practice and multiplayer client based on [MDPro3](https://github.com/daominah/mdpro3fork). Built using Unity, Ultra Duel brings together competitive dueling, deck building, AI opponents, online multiplayer, and player progression in one experience.

Practice your favorite strategies, experiment with different formats, or challenge your friends — all for free.

**TCG Advanced • Genesys • Solo Practice • Online Multiplayer • Deck Editor**

---

## Download Ultra Duel

Choose the version compatible with your system.

| Platform | Download |
|---|---|
| **Windows** | [Download Ultra Duel](https://github.com/MatteoGXC/ultra-duel/releases/latest/download/Ultra-Duel-Setup.exe) |
| **macOS — Intel** | [Download for Intel Mac](https://github.com/MatteoGXC/ultra-duel/releases/tag/macos-v0.1.12) |
| **macOS — Apple Silicon** | [Download for Apple Silicon](https://github.com/MatteoGXC/ultra-duel/releases/tag/macos-v0.1.10) |

**[View All Releases](https://github.com/MatteoGXC/ultra-duel/releases)** | **[Download Guide](DOWNLOADS.md)**

### Windows Installation

1. Download `Ultra-Duel-Setup.exe`.
2. Run the installer and select your installation directory.
3. Allow the installer to download and verify the required files.
4. Launch Ultra Duel using the desktop shortcut.

Unity is not required to play.

### Updating Ultra Duel

Already have Ultra Duel installed?

Download the [Ultra Duel Updater](https://github.com/MatteoGXC/ultra-duel/releases/latest/download/Ultra-Duel-Updater.zip), extract it into your game directory, close the game, and run `Update-Ultra-Duel.cmd`.

The updater downloads changed files while preserving your decks, settings, and local progress.

**Important:** The game executable must remain alongside its required runtime and asset folders.

---

## Game Features

### TCG Advanced & Genesys

Choose between different Yu-Gi-Oh! formats.

- **TCG Advanced:** Practice using the traditional competitive TCG rules and banlist.
- **Genesys:** Build decks using a point-based system with 100-point, 0-point, and custom limits.
- **Per-deck formats:** Assign individual formats to your decks.
- **Format validation:** Decks and available AI opponents are checked against the selected format.

Each deck remembers its selected format and point cap.

### Deck Building

Ultra Duel includes a deck editor for creating, organizing, and testing decks.

- Build and customize your Main, Extra, and Side Decks.
- Manage your deck library directly.
- Assign a format to each deck.
- View format badges for your saved decks.
- Use your decks in Solo and Multiplayer.

### Solo Practice

Test your strategies against AI-controlled opponents, even while offline.

- 14 published TCG AI deck lists.
- 15 published Genesys AI deck lists.
- Casual, Standard, and Hard difficulty categories.
- Random opponent selection.
- Support for custom AI decks.
- Automatic format and point-cap compatibility filtering.

AI opponents are selected based on the rules of your chosen format.

Difficulty categories are approximate and do not represent official tournament rankings.

### Online Multiplayer

Challenge friends through Ultra Duel's hosted multiplayer infrastructure.

**How to play online:**

1. Open **FRIENDS** and register an account.
2. Add your friend using their username.
3. Accept the friend request.
4. Open **DUEL** and create or join a private room.
5. Choose your format and start your duel.

Both players connect through Ultra Duel's servers. Router port forwarding is not required.

Compatible client versions are required for multiplayer.

For technical information, see [Server Documentation](Server/README.md).

### Player Progression

Ultra Duel includes a home screen with several features beyond traditional dueling.

- **DUEL:** Play against friends online.
- **DECK:** Build and manage decks.
- **SOLO:** Practice against AI opponents.
- **SHOP:** Browse cosmetic customization options.
- **FRIENDS:** Manage your friends list.
- **MISSIONS:** Complete gameplay objectives.
- **PROFILE:** Customize your player profile.
- **DUEL PASS:** Earn rewards through gameplay.

The free Duel Pass includes 20 tiers without an expiration date.

Every win awards 100 XP, and every 200 XP unlocks a new tier. Rewards include cosmetic gems, titles, and profile frames.

There are no paid gameplay advantages.

### Card Database & Gameplay

Ultra Duel includes improvements to card availability, format validation, and gameplay behavior.

Previous updates introduced additional card IDs, maintained effect scripts, artwork sources, and database corrections.

Other improvements include:

- Life Point display corrections.
- Damage and activation-cost notifications.
- Card movement and destruction fixes.
- Improved format-aware deck validation.
- Updated card database snapshots.

Card availability does not automatically imply legality in a particular format.

Additional details are available in [Card Update Audit](Data/card-update-audit.json).

### Game Updates

Ultra Duel checks GitHub for new releases and development updates.

Players can use the updater to download changed files rather than reinstalling the entire game.

---

## Development & Source Code

Ultra Duel is an open-source project developed primarily with **Unity and C#**, alongside C++ duel-engine components, Lua card scripts, Python utilities, and other development tools.

Developers interested in examining the project can access its source code directly through this repository.

### Building from Source

Requirements include:

- A compatible Unity Editor installation.
- C# and C++ development tools.
- Python for supporting scripts.
- Required project dependencies and runtime resources.

See [FORMAT-BUILD.md](FORMAT-BUILD.md) for detailed build instructions, dependency information, and runtime configuration.

### Testing

The repository includes automated tests and verification utilities.

```powershell
python Tools/prepare-solo-decks.py
python Tests/NativeServerProtocolTests.py --all-solo
```

Existing verification covers selected areas of deck validation, AI opponent compatibility, multiplayer protocols, Life Point handling, and card movement.

Passing these tests does not guarantee that every possible gameplay interaction is free of errors.

---

## Bug Reports & Feedback

Encountered a bug, found something that isn't working correctly, or have an idea to improve Ultra Duel?

You can contact me through either of these methods:

- **GitHub:** [Open an Issue](https://github.com/MatteoGXC/ultra-duel/issues)
- **Discord:** `matthewtheintelligent`

When reporting a problem, please include your game version, operating system, a description of the issue, and any screenshots or error messages that may help.

All feedback is appreciated and helps improve Ultra Duel.

---

## Support Ultra Duel

Ultra Duel is free to play and maintained as a community-driven project.

Running online multiplayer and related services requires infrastructure, including VPS hosting, network bandwidth, storage, and regular maintenance.

If you'd like to help keep the servers online, voluntary donations may be used to support these operating expenses.

**Supporting the project is entirely optional.** Donations do not unlock exclusive gameplay features, competitive advantages, or paid cards.

*Donation options will be announced when available.*

---

## Credits & Acknowledgments

Ultra Duel is based on the open-source **MDPro3** project and builds upon the work of its original developers and contributors.

Special thanks to the MDPro3 development team and the broader open-source Yu-Gi-Oh! simulator community for their work on the client, duel engine, and supporting technologies.

**Original project:** [MDPro3 Source Repository](https://github.com/daominah/mdpro3fork)

**Source baseline:** `025d2b8`

Upstream development history and applicable copyright and license notices are retained.

---

## License

Ultra Duel's GPL-covered source code is distributed under the **GNU General Public License v3.0 (GPL-3.0)**.

See [LICENSE](LICENSE) for additional information.

Third-party artwork, music, audio, trademarks, and other proprietary materials remain subject to their respective owners' rights and are not automatically licensed under GPL-3.0.

### Disclaimer

Ultra Duel is an independent, unofficial fan-made project.

Yu-Gi-Oh! and its associated intellectual property belong to their respective rights holders, including Konami and Shueisha where applicable.

Ultra Duel is not affiliated with, authorized by, sponsored by, or endorsed by Konami or any of its affiliates.

---

**[Download Ultra Duel](https://github.com/MatteoGXC/ultra-duel/releases) • [Report a Bug](https://github.com/MatteoGXC/ultra-duel/issues) • [View Source Code](https://github.com/MatteoGXC/ultra-duel)**
