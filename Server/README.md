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
docker build -f Server/Dockerfile -t ultra-duel-service:0.1.2 .
mkdir -p state
chmod 700 state
docker run -d --name ultra-duel-service --restart unless-stopped --init \
  -p 9444:9444 -p 7911-7922:7911-7922 \
  -v "$PWD/state:/state" --memory 2g --cpus 2 --pids-limit 128 \
  ultra-duel-service:0.1.2
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

The free Duel Pass is local to each game installation and is stored in
`Progress/profile.json`. It is not a shared competitive currency or leaderboard.
Updaters and installers preserve it, along with deck files, settings and sessions.
