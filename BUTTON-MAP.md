# Ultra Duel — button and screen map

This follows the current application code. Arrows show the result of pressing a button. Buttons that depend on being signed in, owning an item, having an invite, or being the administrator appear only in that state. Repeated card/item/player buttons are grouped by their action. This is a navigation map, not a claim that every path was manually played through.

```text
START SCREEN
└─ GAME START → HOME

HOME
├─ DUEL → Duel-mode menu
│  ├─ ONLINE DUEL
│  │  ├─ Signed out → Sign-in screen
│  │  └─ Signed in → Choose TCG / Genesys (and Genesys point cap)
│  │     └─ Validate selected deck
│  │        ├─ Invalid → Explanation; fix deck before searching
│  │        └─ Valid → WAITING FOR OPPONENT
│  │           ├─ CANCEL SEARCH / Back → Cancel queue → Home
│  │           └─ Opponent found → Confirm connection → Match room
│  │              ├─ LEAVE MATCH → Leave connection/room
│  │              └─ Both ready → Rock–Paper–Scissors → First/second → Duel
│  ├─ FRIEND DUELS → Friend-room browser (sign-in required)
│  │  ├─ MODE: 1v1 / DUOS 2v2 → Toggle room mode
│  │  ├─ Format → Choose TCG / Genesys and cap
│  │  ├─ CREATE ROOM → Create room → Room tables
│  │  ├─ TABLES > on a room → Room tables
│  │  │  ├─ ENTRY on a table → Join that table → Table lobby
│  │  │  │  ├─ Deck case → Choose your deck → Return to lobby
│  │  │  │  ├─ ENTRY → Ready/unready your deck
│  │  │  │  ├─ BEGIN DUEL [host; readiness required]
│  │  │  │  │  └─ Rock–Paper–Scissors → First/second → Duel
│  │  │  │  ├─ Room Details > → Room information → CLOSE
│  │  │  │  ├─ Room Members > → Member information → CLOSE
│  │  │  │  ├─ Invite Friends > → Choose friend → Send invitation
│  │  │  │  ├─ Replays > → Leave table → Replay library
│  │  │  │  ├─ CHAT → Show/hide chat panel
│  │  │  │  └─ Leave Table > / Back → Leave table
│  │  │  ├─ WATCH on a table → Join as spectator
│  │  │  ├─ REFRESH → Reload tables
│  │  │  ├─ DISBAND ROOM [host] → Remove room → Room browser
│  │  │  └─ ALL FRIEND ROOMS → Room browser
│  │  ├─ WATCH on a room → Spectator connection
│  │  ├─ REFRESH → Reload available rooms
│  │  ├─ FRIENDS → Friends screen
│  │  └─ DUEL MODES → Duel-mode menu
│  ├─ DUOS 2v2 → Same friend-room flow, with team seats
│  └─ SOLO → Solo library (same destination as Home → SOLO)
├─ DECK → Deck library
│  ├─ Search → Filter saved decks
│  ├─ Create deck (+) → Enter name → Import clipboard deck if present → Refresh library
│  ├─ Select existing deck → Deck actions / select for current purpose
│  │  ├─ Edit → Deck Editor
│  │  ├─ Copy → Save another deck copy
│  │  ├─ Delete → Confirmation → Delete selected deck
│  │  └─ Category/type actions → Filter, move or copy between categories
│  ├─ ONLINE DECK → Online deck viewer
│  ├─ Confirm [batch delete/move/copy] → Apply selected operation
│  └─ Cancel / Back → Previous screen
├─ SOLO → Solo library
│  ├─ Format → TCG / Genesys and cap → Refresh library
│  ├─ Opponent/deck entry → Show description and available options
│  ├─ AI deck selection → Deck chooser → Return to Solo
│  ├─ Practice options / lock hand → Adjust practice setup
│  ├─ PLAY → Validate setup → Start AI duel
│  └─ Back → Home
├─ SHOP → Accessories shop
│  ├─ MATE / CASE / PROTECTOR / FIELD SETS / FRAME → List that accessory type
│  ├─ < / > → Previous/next item page
│  ├─ [price] GEMS → Purchase item → Refresh ownership/balance
│  ├─ EQUIP [owned frame] → Apply frame
│  ├─ OWNED → Disabled ownership indicator
│  ├─ ACCESSORIES → Accessory listings
│  └─ DUEL PASS → Duel Pass screen
├─ Profile panel → PROFILE
│  ├─ Rank/pass button → Duel Pass
│  ├─ EDIT → Edit Profile
│  │  ├─ EQUIP beside mate → Set profile mate → Profile
│  │  ├─ PROFILE ICON → Icon picker
│  │  │  └─ SELECT → Save icon → Profile + refresh Home portrait
│  │  └─ SHOP → Accessories shop
│  ├─ REPLAY → Match history
│  │  └─ WATCH > [saved replay exists] → Replay playback
│  ├─ DATA → Player statistics
│  │  ├─ MATCH HISTORY → Match history
│  │  └─ DUEL PASS → Duel Pass
│  └─ COLLECTION → Owned accessories
│     └─ SHOP → Accessories shop
├─ NOTICES → Notification inbox
│  ├─ ACCEPT [friend request] → Accept request → Refresh inbox
│  ├─ JOIN [duel invite] → Join invited room/table
│  ├─ VIEW ROOM [room notification] → Room tables
│  ├─ FRIENDS [other social notice] → Friends screen
│  ├─ REFRESH → Fetch notifications
│  ├─ MARK ALL READ → Save read state
│  └─ UPDATES → Updates screen
├─ MISSIONS / mission card VIEW → Missions
│  ├─ ALL MISSIONS / DAILY / LIFETIME / DUEL PASS → Filter mission list
│  ├─ RECEIVE ALL → Claim completed mission rewards → Refresh
│  └─ DUEL PASS → Duel Pass screen
├─ Duel Pass panel → DUEL PASS
│  ├─ < / > → Previous/next reward page
│  └─ CLAIM REWARDS [earned, unclaimed tiers] → Grant rewards → Refresh
├─ FRIENDS → Friends screen (sign-in required)
│  ├─ + beside player-name input → Send friend request
│  ├─ ACCEPT [incoming request] → Add friend → Refresh
│  ├─ JOIN [invitation] → Join invited room
│  ├─ > beside friend → Friend details
│  │  ├─ INVITE TO DUEL → Choose format → Create/reuse room → Send invite
│  │  ├─ REMOVE FRIEND → Confirmation → Remove friend
│  │  └─ BACK → Friends list
│  ├─ X [outgoing/pending entry] → Remove/cancel relationship entry
│  ├─ REFRESH → Refresh friends and requests
│  └─ SIGN OUT → End session → Sign-in screen
├─ MENU → Menu screen
│  ├─ SETTINGS → Game settings
│  │  └─ Setting controls → Change that setting; applicable changes saved
│  ├─ WALLPAPER → Wallpaper appearance picker → Apply selection → Home
│  ├─ MATCH HISTORY → Recent matches → WATCH saved replay
│  ├─ PUZZLES → Puzzle library → Select/start puzzle
│  ├─ CUT-INS → Cut-in viewer
│  ├─ UPDATES → Published release notes (installed notes when offline)
│  │  ├─ CHECK FOR UPDATES → Check GitHub → Update/download guidance
│  │  └─ VIEW RELEASE → Open GitHub releases in browser
│  ├─ GITHUB → Open project repository in browser
│  ├─ EXIT → Application exit flow
│  ├─ GUI SCALE → Scale controls
│  │  ├─ − / + → Change interface scale in 5% steps (70–125%)
│  │  ├─ RESET TO 100% → Restore normal interface size
│  │  └─ GAME SETTINGS → Game settings
│  └─ SAVE BACKUPS → Deck backup screen
│     ├─ SYNC DECKS → Sync account decks with VPS
│     └─ LOCAL BACKUPS → Open local backup folder
└─ ADMIN [owner only] → Administration
   ├─ GEMS beside player → Amount/reason form → CONFIRM GRANT
   ├─ UNLOCK ALL beside player → Unlock cosmetics and Duel Pass
   ├─ UNLOCK MY ACCOUNT → Unlock own cosmetics and Duel Pass
   └─ REFRESH → Reload registered players

SIGN-IN SCREEN
├─ SIGN IN → Authenticate → Friends screen
└─ CREATE ACCOUNT → Register/sign in → Friends screen
   (After login, choose Duel again to enter an online/friend-duel flow.)

DECK EDITOR
├─ Search/filter controls → Filter card results under current deck rules
├─ Select card → Card details and context actions
│  ├─ Add → Add to valid deck section
│  ├─ Remove → Remove selected copy
│  ├─ Main/Side actions → Move between allowed sections
│  └─ Related cards / bookmark / rarity → Corresponding card utility
├─ Format/regulation → Deck rules and legality information
├─ SAVE → Save deck, including #ultra-format: metadata
├─ Appearance → Deck cosmetic picker → Apply selection
├─ Submenu
│  ├─ Reset → Reset editor changes
│  ├─ Sort → Sort deck
│  ├─ Shuffle → Shuffle displayed deck order
│  ├─ Copy → Copy deck
│  ├─ Share → Share/export deck
│  ├─ Test → Opening-hand test
│  ├─ Clear → Clear-deck flow
│  └─ Check [format] Deck → Validate deck → Result message
└─ Back → Deck library / caller (unsaved-change handling applies)

DUEL (actions appear only when legal for the current phase/prompt)
├─ Rock / Paper / Scissors → Submit choice → Resolve or repeat tie
├─ First / Second → Choose starting turn
├─ Card → Legal action menu
│  └─ Summon / Set / Activate / Attack / Change position → Engine prompt(s)
├─ Prompt choice / target / zone / Yes / No → Submit engine response
├─ Phase control → Available phase(s) → Advance phase
├─ Chat → Chat panel
├─ Duel menu → Duel settings / surrender / exit flow
├─ ADMIN [owner only, supported own duel] → Duel administration
│  ├─ SET MY LP → Apply entered LP
│  ├─ SET OPPONENT LP → Apply entered LP
│  ├─ ADD TO MY HAND → Add entered card ID
│  ├─ CLEAR OPPONENT HAND → Move opponent hand to Graveyard
│  ├─ WIN THIS DUEL → End duel with owner victory
│  └─ CLOSE → Close admin panel
└─ Duel ends → Results
   └─ CONTINUE → Exit results → Appropriate menu/room return flow
```

Source: `UltraHomeUI.cs`, `UltraExperienceUI.cs`, `UltraPlayerUI.cs`, `UltraRoomBrowserUI.cs`, `UltraRoomUI.cs`, `UltraNotificationsUI.cs`, `UltraMissionsUI.cs`, `UltraAvatarPicker.cs`, `UltraAdminDuelUI.cs`, `DeckSelectorUI.cs`, `DeckEditorUI.cs`, and `SoloSelectorUI.cs` under `Assets/Scripts/MDPro3/UI/ServantUI/`.

Game settings, cosmetic lists and duel cards contain many dynamic controls. Their repeated entries are grouped above; their availability depends on installed assets, account ownership and legal engine responses. The map does not add buttons or alter the game.
