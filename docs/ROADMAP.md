# ROADMAP.md

Phased delivery plan. Companion to [DESIGN.md](DESIGN.md) (what the game is) and [ARCHITECTURE-MP.md](ARCHITECTURE-MP.md) (how MP and bots work).

## Conventions

- Each phase has: **goal**, **deliverables**, **architectural prerequisites**, **out-of-scope guards**, **verification**.
- "Out of scope" is a scope guard, not a wishlist. If we're tempted to add it mid-phase, defer.
- **Architectural prerequisites are non-negotiable**; gameplay features can shift between phases. Phase 2's prerequisites (command stream, fixed sim tick, seeded RNG) lock the whole lockstep MP plan — getting them wrong here is the most expensive mistake on this roadmap.

## Phase 1 — Prototype (DONE)

**Status**: complete. Commit range `3f218a4..e43780c`.

Delivered: window/loop, camera, tile grid, building placement (Collector/Barracks), starting HQ, resource ticks, HUD, save/load, xUnit coverage. See `docs/ARCHITECTURE.md` and `docs/CLAUDE.md`.

## Phase 2 — Units, combat, and the deterministic foundation

**Goal**: a single-player sandbox where the player builds, trains one unit type, moves units, and watches combat resolve. **No AI opponent yet** — combat is exercised against a hand-placed dummy or via the determinism test rig. This phase exists primarily to lay the architectural foundation for lockstep MP; the gameplay it delivers is a side-effect.

### Architectural prerequisites (do these first)

1. **Command stream**: every gameplay-mutating action becomes a `Command` record. Systems consume commands; they stop reading keyboard/mouse directly. Input becomes a translator (keyboard/mouse → commands). See `ARCHITECTURE-MP.md` §Command stream.
2. **Fixed sim tick (20 Hz) decoupled from render**: sim advances exactly one step per 50ms; render interpolates. `GameRoot.Update` becomes a tick scheduler.
3. **Seeded `MatchRng`**: single RNG per match, seeded at world construction. Never `Random.Shared`. Phase 2 may not use random yet — wire it anyway so Phase 3 doesn't have to retrofit.
4. **`PlayerId` and entity ownership**: `Building` and `Unit` get a `PlayerId`. Phase 2 has one human player + an optional dummy player; the schema supports up to 10.
5. **Determinism discipline in sim code**: no `Math.Sin`/`Cos`/`Atan2`, no `DateTime.Now`, no `Stopwatch`, no hashtable enumeration for gameplay ordering, no allocation-derived IDs. Codified in `ARCHITECTURE-MP.md`.

### Deliverables

- **Soldier** unit type (data in `assets/data/units.json`): HP, damage, range, speed, training time, cost.
- **Barracks training**: queue, progress bar, spawn point (a tile adjacent to the building), cancel.
- **Pathfinding**: A* on the tile grid (4- or 8-neighbour; pick one and document).
- **Selection**: click-to-select single unit, click-drag marquee, shift+click to add, click-empty to clear.
- **Commands**: right-click → move (smart attack-move if hovering enemy unit/building), `A` + click → explicit attack-move, `S` → stop.
- **Combat**: auto-attack closest enemy in range; HP; death removes unit from world. No armor types, no line of sight, no fog of war.
- **HQ HP**: HQ has HP; destruction triggers a "you lose" overlay (no return-to-menu yet — Phase 6).
- **Unit rendering**: coloured quads with team-tinted border, no sprites.
- **HUD additions**: selected unit panel (name, HP), training progress on the selected building.

### Optional but recommended

- **Scripted dummy player**: not an AI — a deterministic script that emits one command every N ticks (e.g. "place Collector at tile X if you have credits"). Its only job is to exercise the command stream from a non-input source so we catch input-coupling bugs before bots show up in Phase 3.

### Out of scope (defer to Phase 3+)

- Any real AI behaviour (build orders, target prioritisation across the map, etc.).
- Multiplayer / networking.
- Multiple unit types or buildings beyond Phase 1's set + Soldier.
- Defense towers, walls, factories.
- Fog of war / line of sight.
- Smooth (sub-tile) unit positions if grid-snap is sufficient.
- Sound, particles, real art.
- Faction asymmetry.

### Verification

- `dotnet test` green; new tests cover pathfinding, combat math, command application, deterministic RNG.
- Can play a 5-minute solo session: spawn → economy → train units → kill the dummy → HQ destroys → "you lose" overlay.
- **Determinism smoke test**: run 1000 sim ticks from a fixed seed + recorded command stream; hash world state; result is identical across two runs (and across Mac/Windows once CI is set up in Phase 3).

## Phase 3 — Bots and the first real game loop

**Goal**: a real solo experience — player versus one scripted bot, with the HQ-destruction win condition active and difficulty tiers selectable.

### Architectural prerequisites

1. **Bots-as-players**: a bot is a `PlayerId` with no input device; a controller class produces `Command`s for it each tick. Identical pipeline to humans from the sim's perspective.
2. **Cross-platform determinism CI**: GitHub Actions matrix (`macos-latest` + `windows-latest`) running a determinism harness that diffs world-state hashes tick-by-tick. Lockstep MP depends on this; we set it up here so Phase 4 doesn't surprise us.

### Deliverables

- **Defense Tower** building (1×1, 150c, fixed attack, no movement). Useful for both bot strategy and base defense.
- **Bot controller**: `BotController.Tick(World, PlayerId, MatchRng) → IEnumerable<Command>`.
- **Three difficulty tiers** (Easy / Medium / Hard) — see `DESIGN.md` §Bot difficulties. Tier differences are behavioural (decision rate, build order quality, target selection), not resource cheats.
- **Skirmish mode**: main menu → pick map, bot difficulty, your team → start. Replaces Phase 2's sandbox.
- **Win/lose screens**: result, match duration, simple stats (units built, lost; credits earned).
- **Match config record**: `MatchConfig(MapId, Players[], Seed)`. Used now for solo skirmish; reused unchanged for MP lobbies in Phase 4.

### Out of scope

- Any networking.
- Multiple maps (single fixed map still).
- More than one bot per match.
- Multiple unit types beyond Soldier (still).

### Verification

- 31+ tests green (existing) plus bot tests: deterministic bot produces identical command stream from same seed + same world.
- Cross-OS CI matrix passes the determinism harness.
- Subjectively: can lose to Hard, can beat Easy comfortably, Medium is interesting.

## Phase 4 — Multiplayer (lockstep)

**Goal**: 2-10 player online matches, peer-hosted, cross-platform (Mac + Windows), bots fill empty slots.

### Architectural prerequisites (mostly already in place from P2/P3)

1. Command stream + deterministic sim — done in P2.
2. Cross-OS determinism CI — done in P3.
3. **Network message framing**: command bundles, lobby messages, sync/heartbeat. Protocol defined in `ARCHITECTURE-MP.md`.
4. **Transport**: `LiteNetLib` (MIT) integrated. Reliable-ordered channel for commands, unreliable for telemetry.

### Deliverables

- **Lobby**: create, join-by-6-char-code, slot config (human / bot-{easy,medium,hard} / closed), team assignment, ready-up, start.
- **Local network discovery**: zero-config LAN play.
- **Internet play via UPnP**: `Mono.Nat` (MIT) opens host port automatically on supporting routers. Fallback: manual port forward + IP entry.
- **Lockstep core**: 20 Hz sim tick; 2-tick input lag; host gathers per-peer commands and broadcasts a unified bundle each tick.
- **Bot slot fill**: same controller as Phase 3. Bot commands are produced by the host peer and merged into the command bundle.
- **Disconnection handling**: pause-and-wait up to 30s; on timeout, AI takes over the dropped player's faction (Medium tier).
- **Surrender command**: ends a player's participation; HQ auto-destroyed.
- **Replay capture**: command stream + match config saved to file; loadable for playback (lockstep makes this nearly free).

### Out of scope

- Centralised matchmaker / public lobby browser.
- NAT hole-punching via rendezvous server (UPnP + manual is enough for friend lobbies; rendezvous is Phase 5+ if requested).
- Voice chat. Text chat is optional (cheap to add given the channel exists).
- More units / buildings — content expansion is Phase 5.

### Verification

- 4-peer end-to-end match completes, Mac host + Windows joiners + 1 bot, no desync over 20 minutes.
- Network kill-test: pull WiFi on one peer → pause → reconnect → game continues.
- Replay of the 4-peer match plays back identically on a fresh machine.

## Phase 5 — Content and maps

**Goal**: enough variety to make 20 matches feel different.

### Deliverables

- **Multiple unit types**: Scout, Marksman (ranged), Tank (Factory-trained). Stats in `assets/data/units.json` (see `DESIGN.md` §Units for proposed numbers — tune in playtest).
- **Factory** building (trains heavy units).
- **Wall** building (cheap, blocks pathing; placement constrained adjacent to owned buildings).
- **Multiple maps**: 3-5 hand-authored maps in `assets/maps/*.json` (terrain, spawn positions, decorations). Map editor not required — JSON by hand is fine.
- **Spawn position support**: per-map, per-team. Replaces Phase 2's "spawn at corner" hardcode.
- **Mini-map**: top-right of HUD, click to pan camera.

### Out of scope

- Procedural map generation.
- Faction asymmetry.
- Map editor UI (defer to Phase 6 or never).

### Verification

- All three difficulty bots play each map adequately.
- New units don't break the determinism harness.

## Phase 6 — Polish

**Goal**: looks and feels like a real game.

### Deliverables

- **Art pass**: real sprites for units/buildings, tile textures, projectile effects.
- **Audio**: unit acknowledgement sounds, attack sounds, build/train cues, music loop per match phase.
- **Tutorial**: scripted single-player intro that walks through controls.
- **Settings menu**: resolution, edge-scroll toggle, audio volumes, keybindings.
- **Quality-of-life**: control groups (Ctrl+1..9 / 1..9), idle-worker hotkey, alert pings, post-match stats.
- **Accessibility**: colourblind-friendly team palette, configurable font size.

### Out of scope

- Steam integration. (Was excluded from Phase 1 scope; still excluded — separate engagement if pursued.)
- Anti-cheat. Lockstep with peer hosts has no anti-cheat story without dedicated servers; document as a known limitation.
- Ranked ladder, accounts, persistence beyond local replays.

## Cross-cutting tracks

These don't fit a single phase; pick them up where they fit naturally.

- **Determinism harness**: stub in P2, CI in P3, stress-test in P4. Owns the credibility of lockstep.
- **Replays**: capture starts in P4, viewer UI in P6.
- **Performance budget**: at 5v5 with ~30 units/player, target 60Hz render on a 5-year-old laptop. Profile in P5.
- **Save/load**: Phase 1 covers single-player; in P3 update to handle skirmish match state; in P4 saves become replays (different beast). Single save slot remains the rule until P6.

## Sequencing rules of thumb

- **Never start a phase without its architectural prerequisites**. They cost less here than after the gameplay is built on top of the wrong foundation.
- **Defer all content** until the system that consumes it works. Adding a new unit type before unit movement works is wasted effort.
- **Test cross-platform early**. The longer we delay running on Windows, the worse the first bug will be. By end of Phase 2 we should have run the prototype on a Windows machine at least once.
