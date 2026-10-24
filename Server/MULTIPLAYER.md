# Multiplayer implementation status

The requested modes are independent requirements:

| Mode | Engine shape | Current status |
| --- | --- | --- |
| 1v1 | Two opposing sides | Existing |
| 2v2 | Two opposing sides, alternating teammates | Existing; regression checked with four TCP clients |
| 2v1 | Two opposing sides, one side has no teammate | Native Windows and Linux turn/discard/surrender tests pass; Apple Silicon checks and live deployment still required |
| 1v1v1 | Three independent opposing players | Requested, **not implemented** |
| 1v1v1v1 | Four independent opposing players | Requested, **not implemented** |

An empty tag seat is not a free-for-all player. Do not expose either free-for-all mode through the current tag server or advertise either as playable.

## Free-for-all work still required

Each participant needs an independent field, deck, hand, graveyard, banished cards, Extra Deck, life points, and turn. A second rendered field alone cannot provide this behavior.

Current constraints verified in source:

- `ocgcore/field.h`: `field::player[2]` and two-player turn counters.
- `ocgcore/common.h`: `check_playerid` only accepts players 0 and 1.
- `ocgcore/libduel.cpp`, processors and card scripts use a single opposing player, commonly `1 - player`.
- The native game-message protocol and Unity duel state represent two sides. Tag mode changes the controller of a side rather than adding a side.
- `Server/service.py` only creates 1v1 or 2v2 workers.

Implementation must update the rules engine and script compatibility, the wire protocol and replay format, server worker routing, and the Unity field and opponent selection. Opponent-sensitive effects must have defined multiplayer behavior. Eliminated players must leave turn and response ordering without terminating the remaining duel or revealing hidden cards.

Acceptance requires actual network games with three and four independent clients covering attack target choice, effect targeting across opponents, response ordering, damage to one player, elimination, continued play, winner selection, disconnects and replay playback. Existing 1v1, 2v1 and 2v2 games must still pass. No free-for-all release has passed these checks.
