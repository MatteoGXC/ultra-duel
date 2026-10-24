# Ultra Duel VPS service

This service supplies accounts, friend requests, presence and private two-player
duel rooms. SQLite persists in `state/friends.sqlite3`. Each duel runs in its own
native process; the Windows Solo listener still defaults to loopback.

The deployed Ubuntu service uses HTTPS TCP 9444 and room TCP ports 7911–7922.
Existing nginx and other VPS containers are unchanged. Players need no port
forwarding: both connect outward to the VPS. Up to 12 rooms run concurrently,
with a one-hour room lifetime. Friend rooms support TCG Advanced, Genesys 100,
Genesys 0 and custom point caps. The server enforces the chosen rules.

Build from the repository root:

```sh
docker build -f Server/Dockerfile -t ultra-duel-service:0.1.3 .
mkdir -p state
chmod 700 state
docker run -d --name ultra-duel-service --restart unless-stopped --init \
  -p 9444:9444 -p 7911-7922:7911-7922 \
  -v "$PWD/state:/state" --memory 2g --cpus 2 --pids-limit 128 \
  ultra-duel-service:0.1.3
```

The container runs as UID 1001; the state directory must be writable by that UID.
`ULTRA_HOST`, `ULTRA_PORT` and `ULTRA_ROOM_FIRST` configure the advertised IP,
HTTPS port and first room port. Configure matching Docker port mappings when
changing them. A certificate and private key are generated in the persistent
state volume on first start. Keep that volume when replacing the container.

The client pins the exact public certificate SHA-256 in
`Data/ultra-services.json`. The private key stays on the VPS. If the public IP or
certificate changes, update the client settings and publish a new game update.
No TLS validation is disabled globally. Passwords are salted and scrypt-hashed;
session tokens are hashed in SQLite. Room secrets are supplied only to the host
and accepted friends, and verified by the native TCP listener before joining.

Use SQLite's backup API to back up the live database rather than copying only
the main file while WAL writes are active. Back up the certificate and private
key separately with private permissions. Stop this container before restoring
its state, then restart it. Other VPS services do not need to be stopped.

Signed-in accounts have a server-side cosmetic wallet, pass progress and owned inventory.
Offline play retains local progression in `Progress/profile.json`. The two wallets
are separate. Wins earn 100 pass XP and 25 cosmetic gems; each 200 XP unlocks a
free grade awarding another 50 gems. There is one free track and no paid tier.
Game outcomes are client-reported for this casual cosmetic system; they are not
an authoritative competitive rating. Purchases, balance changes and claims are
serialized by SQLite transactions and duplicate game IDs cannot award twice.

Owner access is an allowlist in the private state volume: `state/admins.json`
contains an array of existing account names. It is absent from release packages.
While signed in on the allowlisted account, type `ultraadmin` on the home screen.
Every admin request checks the authenticated account on the server; the keyboard
sequence itself grants no permissions. Gem grants are recorded in the `grants`
table with actor, recipient, amount, reason and timestamp.

Accepted friends can receive five-minute duel invitations, and presence records
Home, In duel, In room, Solo or Deck editing. Rooms remain two-player games with
the complete bundled card engine. A four-player free-for-all is not implemented;
the engine's two-field assumptions require a separate core/protocol project.

Updaters and installers preserve decks, replay files, local progress, settings and
account sessions. Keep the persistent state volume when upgrading the service.

Daily missions reset at 00:00 UTC. Daily, Lifetime and Duel Pass mission progress
is derived from the account game ledger and XP. Mission claims are stored with
a per-account unique key (and UTC day for daily claims), so Receive All retries
cannot award a reward twice. Offline mission claims remain local and separate.
