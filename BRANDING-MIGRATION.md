# Ultra Duel runtime and upgrade layout

The Windows application is compiled as `Ultra-Duel.exe` beside `Ultra-Duel_Data`, `UnityPlayer.dll`, native plug-ins and content folders. It runs the Unity game directly. The installer is `Ultra-Duel-Setup.exe`; the updater entry point is `Update-Ultra-Duel.cmd`. Extract the entire updater ZIP, including its PowerShell script and migration DLL.

## Existing installations

Close the game and run the new updater inside the existing game folder. The installer can also update that folder. Known older default installation folders are detected; Browse supports a custom location. Migration retains root `Data`, `Deck`, `Replay` and `Progress` files. Existing personal files take precedence over packaged defaults.

Historical `Matteo-Duel.exe` and `Matteo-Duel_Data` names are accepted only as migration inputs. The earlier `MDPro3-TCG-Genesys` executable/data aliases are accepted too. Unchanged runtime assets move into the canonical data directory; updated game files take precedence. The superseded executables, runtime directories and old updater scripts are archived in `.ultra-migration-backups` inside the selected installation. A migration journal and replaced-file backups support recovery. Updater failures restore replaced files and reverse runtime moves. No deck library is rewritten by this operation.

Personal save snapshots are also stored outside the installation under `%LOCALAPPDATA%/Ultra Duel/UserData`. The updater refuses to modify a running installation. Upgrading through an older updater is supported: the cumulative patch contains the complete Unity boot runtime, which imports unchanged historical assets before loading the game.

## Deck metadata

New saves write `#ultra-format:FORMAT:CAP`. The reader still accepts `#matteo-format:`. Reading an old deck does not alter its bytes. An explicit save replaces the old metadata header while retaining the card list, accessories and format/cap. Canonical metadata takes precedence if both headers occur in a deck.

## macOS

The application remains `Ultra Duel.app`, with its bundled Unity player and `com.ultraduel.launcher` bundle identity. Its existing Application Support location remains unchanged, preserving installed content and local saves. New packaged defaults contain canonical deck headers and macOS instructions. Both Intel and Apple Silicon build configurations use the shared application branding. Updates remain manual DMG replacements.

## Historical files and upstream attribution

Published older releases and their immutable download URLs are retained. Compatibility names remain in migration code and test fixtures to support those installations. User-owned decks, historical build archives, backups and the workspace's parent directory are not renamed in bulk. MDPro3 namespaces, upstream code identities, contributors and license files remain intact.
