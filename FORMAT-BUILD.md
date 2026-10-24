# Ultra Duel — TCG & Genesys

This is a modification of the MDPro3 Unity client, using the source mirror at
https://github.com/daominah/mdpro3fork (baseline 025d2b8).
It preserves the existing scene, animation, cut-in, camera, field, and sound systems.
MDPro3 uses Master Duel assets; it is not an exact copy of every official Master Duel screen or feature.

## Playing

Launch Matteo-Duel.exe and keep its entire folder together.
My Decks opens directly. Open a Deck and click its Format button in the editor
header to assign TCG Advanced or Genesys (100, zero, or a custom point cap).
This is stored inside that Deck's YDK file, survives copying/renaming/exporting,
and is restored when editing or selecting that Deck for Solo.
Older Decks without format metadata default to TCG Advanced.
The bundled Decks are:

- Starter - TCG: Blackwing; use the Blackwing AI opponent.
- Starter - Genesys: Cyber Dragon, exactly 100 points; use Cyber Dragon AI.
- Starter - Genesys Zero: Mokey Mokey, zero points; use Mokey Mokey AI.

Select a Deck before starting Solo. In the room, mark yourself Ready and
press Start once the opponent is ready. Solo automatically loads the AI library
for your selected Deck's saved format. Every listed built-in opponent is checked
against those rules before it is offered; random opponents use that same filtered
library. The Dovphin option accepts your own AI Deck and checks its legality.

Published Solo decks live in `Data/Windbot/Decks/Published`. The catalog in
`Data/solo-published-decks.json` records public Konami Neuron source URLs, author
titles, exact Main/Extra/Side arrays, supported executors, and estimated difficulty.
There are 14 TCG entries and 15 Genesys entries at 100 points: a mixture of Casual,
Standard, and Hard practice. No cards are added, removed, replaced, or optimized.
Run `python Tools/import-published-solo.py` to reproduce and validate these exact
snapshots; `Data/solo-decks-audit.json` records points and file hashes.
Only legal source lists within the current cap are shown. Unsupported archetypes
are unavailable, including Pendulum/Link strategies in Genesys. Smaller caps may
leave no built-in opponents; a legal user-selected Custom AI deck can still play.
The Solo header changes the session format/cap without changing a saved deck profile,
and offers a separate player-deck selector. Random has separate difficulty pools.
Difficulty is estimated; every AI combo and playing strength is not exhaustively tested.

## Format changes

- The Deck editor's Format button offers TCG Advanced, Genesys 100, Genesys zero,
  and a custom Genesys point cap. Solo initially uses the selected Deck's saved format,
  with a session override in its own header.
  Creating a local room still offers session rule selection.
- Both formats use the TCG card pool from the installed card database.
- TCG uses the official Forbidden & Limited snapshot; Genesys instead enforces
  three copies per card and its combined Main/Extra/Side point cap, excluding
  Link and Pendulum Monsters.
- Deck checks run before local duels, readying, AI admission, and side completion.
  Draft decks can still be saved. The Deck submenu includes an explicit check.
- Native local rule IDs 7/8 map to the 2020 Master Rule with TCG hidden-trigger
  ordering. Genesys also removes Extra Monster/Pendulum Zones at engine level.
- The modified client requires matching native DLLs and refuses to silently use
  the original engine for these formats.

## Build

Prerequisites: Unity 6000.0.28f1 with an active eligible license, Microsoft C++
Build Tools with Windows SDK, and premake5 for the native projects.

1. Restore required original font assets and Windows runtime platform bundles.
   Art, closeups, and extra sound packs remain runtime folders alongside the game.
2. Run `python Tools/refresh-format-data.py` to snapshot the official rule pages.
   `Data/format-data-sources.json` records the sources and names unavailable in
   the installed database. Those cards cannot be played until the database and
   scripts are updated together.
3. Generate and build the native solution from `Tools/YGO Classes/build/windows`.
   Replace BOTH `Assets/Plugins/Windows/ocgcore.dll` and `ygoserver.dll` with
   the compiled Release/x64 outputs. Keep their existing Unity metadata files.
4. Run the Unity editor with `-batchmode -projectPath <project> -executeMethod
   WindowsFormatBuild.Build -quit -logFile <log>`.
5. The output is `release/MDPro3-TCG-Genesys/Matteo-Duel.exe` with its
   adjacent runtime files. Keep the whole folder together.

## Verification status

The Windows player and both native libraries build successfully. Verification
includes 18 standalone deck-validator checks, 21 native rules checks, native
field-layout smoke checks, and seven TCP tests against the actual compiled
server and installed card/points data. TCP tests cover a legal 100-point Deck,
over-cap and zero-cap rejection, a legal zero-point Deck, Link exclusion, and
TCG legality. The native server protocol version matches the client and AI.

Art coverage: 14,041 high-resolution images, with 398 newer TCG cards using
the native full-card-image cropping fallback. Every TCG card ID in the
installed database has an image. Provenance and coverage are recorded in
Data/art-data-sources.json.

Packaged Windows runtime checks pass for both formats: menu, format picker,
Deck validation/rendering, real local-server/AI handshake, and duel entry.
The Genesys field, mate, Decks, and opening hand render from the original
asset bundles. The opt-in --format-smoke diagnostics capture scene and UI
separately because the card renderer maintains its own render target.
Full TCG
judge-equivalent coverage of every card is not established by these tests.
The card database comes from the April 2026 source snapshot; rule data is
current to October 19, 2026, and missing new card names are recorded in
Data/format-data-sources.json. Optional character voice packs are unavailable;
core music and effects are included through the original Addressables.

## Ultra Duel home and LP display update

The home screen now prioritizes Solo and Decks, with a current-format/deck panel
and compact shortcuts. The app and Windows launcher are branded Ultra Duel;
the existing release directory is retained so saved Decks and settings stay in place.
Upstream source attribution and internal protocol identifiers are preserved.

LP animations cancel older counts before showing a new total. Starting LP values
are mapped to the local player when going second, consistently in the game, log,
and voice state. Damage notifications distinguish damage from card activation costs.
The native regression checks cover a normal 400 ATK direct attacker from both
engine seats under TCG and Genesys. Runtime checks exercise 400-damage packets
from both seats and overlapping LP animations, checking the actual HUD text.
The originally reported 400-damage attack / 800 self-damage sequence could not
be reproduced without its card or replay; diagnostics now record recipient,
amount, and resulting LP in Player.log for investigating a recurrence.

The later hand/field desynchronization was reproduced: bundled card faces can
lack the optional face-preview Animator. SetCode previously threw before the
Move message could transfer a summoned monster from hand to field, leaving it
revealed in hand and causing later destruction to look like hand destruction.
The optional Animator and disquiet effect are now null-safe. Runtime regression
checks cover summon out of the opponent hand, unchanged hand membership during
damage, and destruction of that exact field monster into the Graveyard.

## Attribution

MDPro3 and its dependencies retain their original licenses and credits. The
earlier MDPro3 mirror includes GPL v3; bundled libraries have their own notices.
Konami/Shueisha artwork and audio remain the property of their respective owners.
This local fan modification is not affiliated with or endorsed by Konami.


## Ultra Duel v0.1.2

The home screen is built by `UltraHomeUI.cs`. Friends and private remote rooms
use the separately deployed service in `Server/`; see `Server/README.md`.
`UltraProgress.cs` awards the free local Duel Pass only on actual duel wins,
excluding spectators, replays, puzzles and hand tests. The profile lives in
`Progress/profile.json` and must never be included in an update archive.

`Tools/refresh-card-data.py` uses pinned BabelCDB and compatible Fluorohydride
script snapshots, keeps existing translations and legacy artwork IDs, corrects
release availability and rebuilds the English Burst Protocol index. The audit
and provenance are in `Data/card-update-audit.json`; scripts were initialized
through the real engine for all 14,625 non-token entries, with zero errors.
`Tests/UltraServiceTests.py` tests HTTPS accounts/friends and two-player public
native rooms. `Tests/UltraRemoteUnityPeer.py` pairs the packaged Windows client
with a remote peer, including Genesys 0. `--ultra-features --ultra-online` on the
existing format smoke test exercises card loading, artwork, TLS pinning, account
creation, pass claims, persistence and the home panels in the real player.

Build the player with `WindowsFormatBuild.RecompilePlayer`, then run
`Tools/Build-Ultra-Launcher.ps1`. Keep `build-info.json` and
`Data/installed-version.json` current. `Tools/package-brand-update.py` creates a
cumulative update against the original public 0.1.0 archives; both 0.1.0 and
0.1.1 installations can apply it directly. The full installer reuses the
original versioned base archives and applies the cumulative update last.
Do not replace the immutable original release assets.


## v0.1.3 player update

The home page keeps the selectable wallpaper clear on the right and moves news
into Updates. It opens from Game Start and provides a profile card, animated mate,
statistics, one free Duel Pass track, earned-gem accessories, a friends drawer,
private invitations, room tables, match history and post-duel rewards.
`Data/shop-catalog.json` must accompany the player build. Four hundred forty-four
existing cosmetics are catalogued; a field set includes its matching base and
parts, using starter components where no matching component exists. Accessories
are available for preview in the shop and require ownership in the appearance
picker. Starter items and wallpapers remain available.

The bundled native core's chain prompt puts a forced byte in the header, whereas
the MDPro protocol puts it on each candidate. `ChainPrompt` handles both forms;
the one-candidate equal-length case uses the known native server flag. Card
confirmation messages also accept both headers. These fixes cover the reported
Sage freeze and reveal/search exceptions without changing the card recipes.
All 29 published Solo entries passed actual mirrored TCP checks through turn 7
or earlier duel completion, with no freezes or protocol exceptions.
This checks execution and protocol progress, not optimal play of every combo.

Run `Tests/UltraPlayerFeaturesTests.py` for account/ownership/authorization checks
and compile `Tests/ChainPromptTests.cs` together with `ChainPrompt.cs` for packet
layout tests. The packaged player accepts opt-in `--format-smoke <folder> TCG
--solo-ai-audit` to exercise every published AI list through the native server.

## v0.1.4 menu update

Custom home pages now reveal the same selected native background as the Deck
page, including its animated particles. They use matching perspective/slide
transitions and circular back buttons. The home icon shader inverts the
background beneath each glyph; captions retain a contrasting edge. Home profile,
mission and pass cards share a left edge. The gem wallet has a bordered frame.
Missions provide persistent Daily, Lifetime and Duel Pass rewards, with atomic
server claims and separate offline progression. The room browser uses compact
room cards and a separate options/deck preview column. Dragging a card out of
the deck now refreshes all collection/detail count indicators.

The original native ClassicRed background (Back0011) is selected once on update;
the original glow and particles remain the shared menu background.
The black background uses neutral gray diagonal bands; its glow and sparks
follow the selected wallpaper's dominant saturated color, sampled from its
thumbnail. A camera-local color volume keeps the native
animation, wallpaper art, UI colors and duel field independent of this tint.
Results reveal each score row, count up the total, reveal rewards and animate earned XP. Gem
art is Konami's Master Duel gem from
https://yugioh-duelist-point.konami.net/assets/img/MD_Gem.png.

Collection entries collapse alternate printings by card name. The collection
search checks names only; it does not match effect text or archetype labels.
Card details say Archetype. `Tools/card_archetype_compat.py` aligns every mapped
card's setcodes in all ten locales with the pinned MyCard database used by the
native scripts, and verifies the English archetype names against that source.
This corrects Kewl Tune Reco, Branded Opening's Despia filter and Titaniklad's
Dogmatika filter. The audit records 2,278 corrected IDs. New unnamed subfamilies
fall back to their named parent archetype. `Tests/NativeArchetypeTests.py` executes
the reported activation and target filters through the actual native Lua API.
An optional Extended card-art setting crops the existing artwork to borderless
card proportions and takes effect on restart. Standard card frames remain the
default. No artwork is synthesized by this option.
