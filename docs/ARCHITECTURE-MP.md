# ARCHITECTURE-MP.md — networking, determinism, and bots

How the game talks to other peers and runs AI. Companion to [ROADMAP.md](ROADMAP.md) (when we build each piece) and [DESIGN.md](DESIGN.md) (what's being communicated).

## Goals (and non-goals)

**Goals**:
- Peer-hosted multiplayer, cross-platform (macOS arm64 + Windows x64), no central servers.
- Bots indistinguishable from human players from the sim's perspective.
- Replays for free.
- Bandwidth scales with player count, not unit count.

**Non-goals**:
- Anti-cheat. Lockstep with peer hosts is structurally trustless; a determined cheater on the host can manipulate the sim. We accept this for a friend-lobby game.
- Dedicated servers / matchmaker. Friend-invite only.
- Cross-version play. All peers must run the same build.

## Model: lockstep deterministic simulation

Every peer runs the **same** simulation. The only thing that crosses the network is **player input**: a stream of `Command` records.

One peer is designated **host** — the input coordinator. Each tick the host collects every peer's pending commands, builds a unified `CommandBundle`, and broadcasts it to all peers (including itself, for symmetry). All peers apply the bundle at the agreed tick and compute identical world state.

```
   Player 1                     Player 2 (host)               Player 3 (bot)
      │ press B, click             │                              │
      │ → PlaceBuildingCmd         │                              │
      │ ─────────────────────────► │                              │
      │                            │  ◄── PlaceBuildingCmd ─────  │
      │                            │ TrainUnitCmd                 │
      │                            │ bot AI → MoveUnitsCmd        │
      │                            │ (gather all for tick N+2)    │
      │                            │                              │
      │ ◄── CommandBundle(N+2) ─── │ ─── CommandBundle(N+2) ────► │
      │ apply at tick N+2          │ apply at tick N+2            │ apply at tick N+2
      ▼                            ▼                              ▼
   identical world state on all peers after tick N+2
```

**Bandwidth analysis**: a typical action (place building, issue move order) is ~16 bytes serialised. At a busy moment a player issues ~2 commands/sec. With 10 players, ~20 commands/sec × 16 bytes = 320 B/s × broadcast to 9 peers ≈ 2.9 KB/s upload at host. Negligible compared to unit-replication models that would push hundreds of KB/s at this scale.

## Tick model

| Clock | Rate | Purpose |
| ----- | ---- | ------- |
| **Sim tick** | 20 Hz (50ms) | Sim advances exactly one step per tick. |
| **Render tick** | up to 60 Hz | Renders interpolated state between sim ticks. |
| **Network tick** | 20 Hz (aligned with sim) | Host broadcasts current bundle each tick. |

**Input lag**: 2 ticks (100ms). When a player issues a command at sim tick N, it's tagged for tick N+2. This window lets the host receive every peer's commands before broadcasting the bundle for that tick. Higher input lag = more tolerance for network jitter; lower = snappier feel. 2 is the default; we can bump to 3 if jitter is a problem.

**Catch-up**: if a peer falls behind (slow CPU or hitch), it runs sim ticks back-to-back without rendering until it's current. The render thread keeps showing the most recent interpolated frame.

**Lag freeze**: if any peer is more than K ticks behind, the host pauses sim advancement until that peer catches up. Show a "Waiting for {name}…" overlay. Default K = 10 (500ms).

## Command stream

Every gameplay-mutating action is a `Command`. Systems consume commands; they never read keyboard/mouse directly. Input becomes a translator (KB/mouse → commands).

```csharp
public abstract record Command(int PlayerId);
public sealed record PlaceBuildingCommand(int PlayerId, string DefinitionId, TileCoord Tile) : Command(PlayerId);
public sealed record TrainUnitCommand   (int PlayerId, int BuildingEntityId, string UnitDefinitionId) : Command(PlayerId);
public sealed record MoveUnitsCommand   (int PlayerId, int[] EntityIds, TileCoord Target) : Command(PlayerId);
public sealed record AttackTargetCommand(int PlayerId, int[] EntityIds, int TargetEntityId) : Command(PlayerId);
public sealed record CancelTrainCommand (int PlayerId, int BuildingEntityId, int QueueIndex) : Command(PlayerId);
public sealed record SurrenderCommand   (int PlayerId) : Command(PlayerId);
```

A `CommandBundle` is the per-tick wire format:

```csharp
public sealed record CommandBundle(int Tick, Command[] Commands, ulong WorldStateHash);
```

`WorldStateHash` is a 64-bit hash of the world state at the start of tick N (sent with bundle for tick N+2). Peers compare hashes after applying each bundle; **divergence → desync, halt and dump a diff**.

## Determinism — what we must and must not do

The whole stack collapses if peers compute different world state from the same inputs. Cross-platform determinism in .NET 8 is achievable but requires discipline.

### Forbidden in sim code

- `Math.Sin` / `Cos` / `Tan` / `Atan` / `Atan2` / `Exp` / `Log` / `Pow` (non-integer exponent). These have implementation differences across runtimes/CPUs.
- `float.Parse` / `double.Parse` on culture-sensitive strings. Use invariant culture if parse is necessary at all.
- `Random.Shared`, `new Random()` (system-seeded), `Guid.NewGuid()`.
- `DateTime.Now`, `DateTime.UtcNow`, `Stopwatch`, `Environment.TickCount`.
- Iterating `Dictionary<K,V>` or `HashSet<T>` for gameplay-affecting decisions. Enumeration order is *implementation-defined*. Use sorted collections or iterate by sorted key.
- LINQ operators with non-deterministic ordering: `GroupBy` (order of groups), `OrderBy` on equal keys (stable but rely on input order). Prefer explicit sorts on unique keys.
- Reading files, the clock, env vars, or network state inside the sim.
- Floating-point auto-vectorisation that reorders operations. Compile with default JIT settings; do not enable AVX-512 selectively.
- `MathF.Sqrt` is OK (IEEE 754 specified). FMA (`Math.FusedMultiplyAdd`) is OK across platforms in .NET 8 — but avoid mixing with non-FMA paths for the same operation.

### Required in sim code

- All randomness flows through a single per-match `MatchRng` (xorshift64\* or PCG, seeded from `MatchConfig.Seed`). Threading: sim is single-threaded.
- Entity IDs come from `World.NextId()`, monotonically increasing, deterministic.
- Vector math via dot products, squared distances, integer comparisons. For "is point in range," compare squared distance to squared range; never call `Sqrt` and compare.
- Iterations over entity collections go in `Id` order. Don't rely on insertion order being stable across peers (it won't be if a peer joined late and rebuilt from a different command stream).
- Position storage: integer tile coordinates + (if smooth movement is added) Q24.8 fixed-point sub-tile offsets. **No floats in position storage** once we go MP.

### Allowed outside sim

Rendering, HUD, audio, input translation. These code paths can use `Math.Sin`, system clocks, and floats freely — they don't affect simulation state.

The rule: **anything that writes to `World` runs in sim and follows determinism rules; anything that only reads `World` is presentation and doesn't.**

## Determinism test harness

We don't ship without it. Two layers:

1. **In-process tick-replay test (Phase 2)**: a `DeterminismTests` class constructs a `World` from a fixed seed, applies a recorded `CommandBundle[]` over N ticks, hashes the world after each tick, asserts the sequence matches a recorded reference hash file. Catches single-machine regressions instantly.

2. **Cross-OS CI matrix (Phase 3)**: a GitHub Actions workflow runs the same test on `macos-latest` (arm64) and `windows-latest` (x64). Both must produce identical hashes. If they ever diverge, the build fails. This is the load-bearing test for the entire lockstep plan — it's the difference between "we believe it's deterministic" and "we know it's deterministic on the platforms we ship."

3. **Live desync detection (Phase 4)**: every `CommandBundle` carries a `WorldStateHash` from the host. Peers verify their own hash matches after applying the bundle. Mismatch → halt the match, dump both peers' world state to a file, ask the user to send it in. Cheap insurance against bugs we didn't catch in CI.

## World state hashing

The hash must be order-independent or use a canonical order. Default: sort entities by `Id`, hash a packed binary representation:

```
hash = xxHash64 (
    Tick,
    Credits[playerId in PlayerId-order],
    [foreach building sorted by Id: Id, PlayerId, DefnId, TileX, TileY, HP],
    [foreach unit sorted by Id:     Id, PlayerId, DefnId, TileX, TileY, HP, OrderState],
    ResourceCarry[in PlayerId-order]
)
```

`xxHash64` is fast and available via `System.IO.Hashing.XxHash64` (in `System.IO.Hashing`, MIT). Use that, not a custom hash.

## Transport

- **Library**: [`LiteNetLib`](https://github.com/RevenantX/LiteNetLib) — MIT, cross-platform, UDP with reliability layer. Mature, used by indie .NET games.
- **Channels**:
  - `ReliableOrdered` for `CommandBundle` and lobby messages.
  - `Unreliable` for ping/heartbeat.
- **Packet size budget**: keep `CommandBundle` under 1200 bytes (one MTU-safe UDP packet) at p99. If we ever exceed it, fragment-with-reassembly is in LiteNetLib but adds latency.

## Connection model

**Star topology** with the host as hub. 10-player game = host has 9 connections, joiners have 1 connection each (to host).

Why not full mesh: at N peers full mesh is N(N-1) connections. With 10 peers that's 90. Each peer would need to authenticate, NAT-traverse, and maintain state with every other peer. Star is dramatically simpler and the host's bandwidth budget at 10 peers is well within consumer ISP upload (~2-5 KB/s per peer, scales linearly).

Host failure handling: **host migration is out of scope through P5**. If the host disconnects, the match ends. Documented limitation. P6 could add migration if we feel it's needed.

## NAT traversal

- **Local network**: zero-config — LiteNetLib's discovery broadcasts find the host on the same subnet.
- **Internet (default)**: UPnP via [`Mono.Nat`](https://github.com/mono/Mono.Nat) (MIT). On a router with UPnP enabled (most home routers), the host's port opens automatically.
- **Fallback**: manual port forward (5557 default, configurable in settings) + IP-direct entry by joiners. Document in README.
- **Future (P5+ if requested)**: tiny rendezvous server we'd run (~$5/mo VPS) for STUN/TURN-style assistance. Not needed for friend lobbies on home networks; useful only if UPnP keeps failing in practice.

## Lobby protocol

Lobby messages are distinct from sim messages; they're plain reliable-ordered records.

```csharp
public sealed record LobbyJoinRequest  (string InviteCode, string PlayerName);
public sealed record LobbyJoinAccepted (int PlayerId, LobbyState State);
public sealed record LobbyState        (string MapId, MatchMode Mode, LobbySlot[] Slots);
public sealed record LobbySlot         (int SlotIndex, SlotKind Kind, int Team, string? PlayerName, BotDifficulty? Difficulty);
public sealed record LobbyConfigChange (LobbySlot[] Slots, string MapId);  // host-only
public sealed record LobbyReadyChange  (int PlayerId, bool Ready);
public sealed record LobbyStart        (MatchConfig Config);

public enum SlotKind { Closed, Human, Bot }
public enum BotDifficulty { Easy, Medium, Hard }
public enum MatchMode { OneVOne, TwoVTwo, FourVFour, FiveVFive, FFA }

public sealed record MatchConfig(
    string MapId,
    int Seed,
    Player[] Players);

public sealed record Player(int PlayerId, int Team, string Name, PlayerKind Kind, BotDifficulty? Difficulty, ColorRgb Color);
public enum PlayerKind { Human, Bot }
```

### Lobby lifecycle

1. **Host creates**. `Host` opens a port (UPnP) and starts listening. Generates a 6-character invite code (`base32(rand(30 bits))`) and shows it.
2. **Joiner enters code**. Client tries LAN discovery first; if no match, looks up the code via rendezvous (P5+) or falls back to manual IP input.
3. **Join handshake**: `LobbyJoinRequest` → host assigns next available human slot → `LobbyJoinAccepted` with full state.
4. **Host configures**: drag/drop slot assignments, sets bot difficulty for empty slots, picks map. Broadcasts `LobbyConfigChange` on every change.
5. **Ready up**: each joiner sends `LobbyReadyChange(ready=true)`. Bots are always "ready."
6. **Start**: host broadcasts `LobbyStart(MatchConfig)`. Every peer constructs a `World` from `MatchConfig` (same seed → same world). Sim ticks begin.
7. **In-match**: command stream as described above.
8. **End**: when a `MatchEndedCommand` is in the bundle (issued by the sim when win cond hits), every peer transitions to a result screen and returns to lobby.

## Bot integration

Bots are first-class players in the sim. The only difference is that a human's commands come from `InputManager` and a bot's commands come from `BotController`.

```csharp
public interface IBotController
{
    BotDifficulty Difficulty { get; }
    IEnumerable<Command> Tick(World world, int playerId, MatchRng rng);
}
```

- **Bot ownership**: by default, **the host runs all bots** for the match. Their commands are produced during the host's pre-bundle gather step. Reason: simpler protocol, easier to keep bot determinism contained. (Sharding bots across peers is an optimisation if host CPU is a bottleneck — not before measurement says so.)
- **Bot determinism**: every bot's `Tick` is pure with respect to `World + MatchRng`. Same world + same RNG state → same commands. No clock reads, no system random. Bots are part of the deterministic sim, not adjuncts to it.
- **Disconnect takeover**: if a human disconnects beyond the timeout, a Medium bot is instantiated for their `PlayerId` mid-match. The bot's commands flow through the same path as before, just sourced from a different brain. No state migration needed because the sim doesn't know who's flying which seat.

### Bot architecture (overview)

A bot has three layers, top-down:

1. **Strategic** (every ~5s): macro state — econ vs army ratio, expansion timing, when to push. Output: high-level intent ("expand", "build army", "attack").
2. **Tactical** (every ~1s): translate intent into build/train queue and army groupings. Output: queued commands for the next strategic window.
3. **Reactive** (every tick if needed): emergency responses — defend if HQ under attack, retreat damaged units, kite vs heavy units. Output: immediate commands.

For Phase 3, expect to ship a single bot script with three difficulty *parameter sets* (not three different bots). Real bot-personality diversity is Phase 6+ if we want it.

## Replays

Lockstep gives us replays nearly free.

- **Recording**: every host writes the `MatchConfig` and every `CommandBundle` it broadcasts to a file (`replays/{date}-{matchid}.crep`).
- **Playback**: load `MatchConfig`, reconstruct `World`, feed bundles through the same sim path. Render normally; no input.
- **Size**: minutes-long match = tens of KB.
- **Forward compatibility**: replays break when build changes. Document; don't try to support cross-version playback through P5.

## Phase-by-phase responsibilities

| Concern                          | Where it lands |
| -------------------------------- | -------------- |
| Command-stream architecture      | Phase 2        |
| Fixed sim tick + decoupled render | Phase 2       |
| Seeded `MatchRng`                | Phase 2        |
| Determinism rules codified       | Phase 2        |
| In-process determinism test      | Phase 2        |
| Cross-OS determinism CI          | Phase 3        |
| Bot controller architecture       | Phase 3       |
| LiteNetLib transport             | Phase 4        |
| Lobby protocol                   | Phase 4        |
| Lockstep tick scheduler          | Phase 4        |
| UPnP + manual port fallback      | Phase 4        |
| Live desync detection            | Phase 4        |
| Replay capture                   | Phase 4        |
| Replay viewer UI                 | Phase 6        |
| Rendezvous server (optional)     | Phase 5+       |

## Known limitations & accepted risk

- **No anti-cheat**. Peer-hosted lockstep is structurally trustless. A cheating host can manipulate the sim. We accept this for a friend-lobby game.
- **No host migration**. If the host disconnects, the match ends. P6 to add if needed.
- **Cross-version play not supported**. Builds must match exactly.
- **NAT traversal is best-effort**. UPnP covers most home routers; users behind double-NAT or CGNAT may need manual port forwarding or a third option (Hamachi/ZeroTier).
- **Sub-tile movement deferred**. Grid-snap through P2 is uglier but vastly easier to keep deterministic. Revisit when (or if) it hurts the feel.
