# Premade archetype recipes

These are basic starting points for learning an archetype, with separate TCG,
Genesys 100 and Genesys 0 entries. They are not tournament recommendations.

Most base recipes come from [anonfansub/ygodecks](https://github.com/anonfansub/ygodecks),
pinned to commit `950dc0f2452e5e79c48398efc607e6fdd94c4215`. The source collection
focuses on demonstrating individual strategies. A few additional recipes use
bundled WindBot lists; the Kewl Tune starters were authored for this library.
Each catalog entry records its source and every removed or added card. Numeric
source snapshots are retained in `source-recipes.json`.

The builder applies the installed TCG pool, banlist, copy limits and Genesys
points, preserves the recipe's core, and rejects variants that would lose too
much of their engine. Unavailable entries explain their constraints; that does
not claim no other possible build exists. Genesys excludes Link and Pendulum
Monsters. Some archetypes have no functional zero-point starter here.

Run `python Tools/build-premade-decks.py --archive PATH_TO_PINNED_SOURCE_ZIP`
from the project to reproduce the catalog. The native client validates every
available entry again before previewing or saving it. Templates are read-only;
Save a Copy creates a local deck without replacing an edited deck.

Card images continue to use the separately verified yugiohcardlist source.
