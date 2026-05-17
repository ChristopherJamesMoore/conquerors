# DESIGN.md — gameplay design

What the game is. Companion to [ROADMAP.md](ROADMAP.md) (when we build it) and [ARCHITECTURE-MP.md](ARCHITECTURE-MP.md) (how it talks to other peers and bots).

Numbers and tables here are **proposals, not commitments**. They'll move during playtest. The shape and structure should hold; the values shouldn't.

## One-line pitch

Top-down RTS where 2-10 players build bases on a tile grid, train armies, and destroy each other's HQs. Bots fill empty lobby slots so friends with mismatched availability still play.

## Core loop (60-second pitch)

1. Spawn with an HQ + 500 starting credits.
2. Build **Collectors** to expand income.
3. Build **Barracks** (later **Factory**, **Defense Tower**) to grow your army.
4. Train units, move them, attack enemies.
5. Destroy enemy HQs.
6. Last team standing wins.

The loop is short on purpose — Conquerors-style RTS is a 10-20 minute commitment, not a 1-hour 4X. Match pacing reflects that.

## Match shapes

| Mode | Players | Target match length | Notes |
| ---- | ------- | ------------------- | ----- |
| 1v1  | 2       | 6-12 min            | Tightest balance test. |
| 2v2  | 4       | 10-20 min           | The "Friday night" case. |
| 4v4  | 8       | 15-25 min           | Bigger maps, more interesting macro. |
| 5v5  | 10      | 20-30 min           | Stretch goal; host machine load is real (see `ARCHITECTURE-MP.md`). |

Free-for-all variants of 4 and 8 are nice-to-have, not core. Teams are the default.

## Players

- A **player** owns an HQ, a colour, a team, and a starting position.
- **Team** is independent of player count: 5v5 is two teams of five; 4-player FFA is four teams of one.
- **PlayerId** identifies a player throughout the match. Bots and humans share this concept; the sim doesn't distinguish them.

## Economy

- **One resource: credits**. Deliberate. A second resource (mineral/gas, food/wood/gold) is the easiest way to bloat a prototype.
- **Income sources**:
  - HQ: +1c/s (set in `buildings.json`).
  - Collector: +2c/s. No location restriction in P2; later we may require placement near a "node" on the map.
- **Sinks**: building cost, unit training cost. No upkeep — that's a tuning lever for later.
- **Starting credits**: 500. Enough to place one Barracks + one Collector.

## Buildings

Numbers are proposals.

| Building       | Cost | Size | Role                          | Introduced |
| -------------- | ---- | ---- | ----------------------------- | ---------- |
| HQ             | n/a  | 3×3  | Anchor, +1c/s, win-cond target | Phase 1   |
| Collector      | 100  | 2×2  | +2c/s                          | Phase 1   |
| Barracks       | 200  | 3×2  | Trains Soldier                 | P1 / P2  |
| Defense Tower  | 150  | 1×1  | Auto-attacks in range          | Phase 3   |
| Factory        | 300  | 3×3  | Trains heavy units (Tank)      | Phase 5   |
| Wall           | 20   | 1×1  | Blocks pathing                 | Phase 5   |

Buildings are static. No rotation, no upgrade tree, no demolition for credits (a Phase 6 nice-to-have if we feel it's missing).

**Placement rules**:
- Must be inside the map.
- All footprint tiles must be unoccupied.
- (Phase 5+) Must be within proximity of an owned building, OR adjacent to an owned Wall.
- Cost is deducted on placement, not on completion. (Build time is a Phase 6 polish item; for now, instant.)

## Units

Numbers are proposals. Tune in playtest.

| Unit     | Trained by | Cost | Train (s) | HP  | DMG | Range (tiles) | Speed (tiles/s) | Phase |
| -------- | ---------- | ---- | --------- | --- | --- | ------------- | --------------- | ----- |
| Soldier  | Barracks   | 30   | 5         | 50  | 10  | 1             | 2.0             | P2    |
| Scout    | Barracks   | 20   | 3         | 25  | 5   | 1             | 3.5             | P5    |
| Marksman | Barracks   | 60   | 8         | 35  | 15  | 4             | 1.5             | P5    |
| Tank     | Factory    | 200  | 15        | 200 | 25  | 2             | 1.0             | P5    |

**Targeting**:
- Auto-attack the closest enemy unit/building in range when idle.
- An explicit attack command overrides until the target is dead or out of reach.
- Move commands cancel attack intent.

**Death**: removed from world. No corpses, no kill rewards.

**Pathing**: A* on the tile grid (4-neighbour for P2 — simpler and more visibly RTS-like; 8-neighbour is a P5 consideration). Path replanning when blocked; give up if no path after K attempts (avoids hangs against walled bases).

**Sub-tile positions**: P2 ships grid-snap movement (unit always centred on a tile). If it feels too jittery we revisit in P3 with sub-tile interpolation (renders smoothly, sim still grid-aligned).

## Combat

- **Auto-attack** when a valid target is in range and the unit isn't busy moving (configurable per unit later).
- **Damage**: flat integer per attack. No armor types in early phases.
- **Attack rate**: 1 attack per second baseline; per-unit overrides in JSON when needed.
- **Line of sight / fog of war**: **none** through Phase 4. Phase 5 introduces fog of war if it earns its complexity.
- **Friendly fire**: no.
- **HQ HP**: 1000. Tower attacks: 10 dmg/s. Soldier attacks: 10 dmg. So one Soldier alone takes 100 seconds to kill an HQ — designed to require armies, not lone snipers.

## Win condition

- A player is **eliminated** when their HQ HP reaches zero.
- A team is **eliminated** when all of its players are eliminated.
- The **last team standing** wins.
- **Surrender** (Phase 4): a player can surrender at any time; treated as HQ destroyed.
- **Stalemate** (Phase 6+): if no combat for N minutes, declare a score-based tiebreaker (units built / kills / credits banked). Not in P2-P5; just shouldn't happen in practice.

## Map

- **Phase 2**: single fixed map (the current 64×64 grid). Player spawns at one corner; "dummy" spawn at the opposite corner.
- **Phase 3**: per-player spawn positions; bot uses the opposite spawn.
- **Phase 5**: multi-map support. Map format = JSON: dimensions, spawn positions per team slot, optional terrain types (impassable, slow), optional decorations.
- **Map size guidance**: 1v1 → ~48×48. 2v2 → ~64×64 (current). 4v4 → ~96×96. 5v5 → ~128×128. Scales roughly with player count.

Symmetric maps only through P5. Asymmetric / "interesting terrain" is Phase 6.

## Selection and control

- **LMB (single click)**: select unit or building. Replaces current selection.
- **LMB drag**: marquee select. Selects all friendly units in the rectangle. Shift-drag adds; ctrl-click subtracts.
- **RMB**: smart command. If hovering enemy → attack; if hovering ground → move; if hovering owned building → guard (Phase 5+).
- **`A` then LMB**: explicit attack-move (move toward target tile; engage any enemy on the way).
- **`S`**: stop. Clear orders.
- **Shift + RMB**: queue command (Phase 3+).
- **Ctrl + 1..9**: assign current selection to a control group.
- **1..9**: select that control group.
- **Double-tap group key**: select group AND centre camera on it.
- **Tab**: scoreboard overlay (MP).
- **Space**: jump camera to last alert.
- **F5/F9**: save/load (singleplayer only; MP replays handled separately).

## HUD

| Panel                      | Phase 1 | Phase 2 additions                    |
| -------------------------- | ------- | ------------------------------------ |
| Credits                    | ✓       |                                      |
| FPS                        | ✓       |                                      |
| Edge-scroll toggle         | ✓       |                                      |
| Build mode + selected defn | ✓       |                                      |
| Selected unit/building     |         | Name, HP bar, training queue         |
| Build / train menu         |         | Buttons when an HQ/Barracks selected |
| Alert log                  |         | "Under attack at X" (P3+)            |
| Mini-map                   |         | Top-right; click to pan (P5+)        |
| Scoreboard                 |         | Tab overlay (MP) (P4+)               |

## UX flow

```
[Main Menu]
  ├─ Singleplayer
  │    └─ [Skirmish setup] → map, bot difficulty, your team → [Game]
  ├─ Multiplayer
  │    ├─ Host → [Lobby] → map, slot config (human/bot-X/closed), team
  │    │     → Start → [Game]
  │    └─ Join → enter code → [Lobby] → ready → [Game]
  ├─ Settings
  └─ Quit

[Game] → win/lose → [Result screen] → Return to lobby / menu
```

P2 ships only `[Game]` (sandbox). P3 adds the skirmish setup screen and result screen. P4 adds the multiplayer branch.

## Bots — design view

Architecture is in `ARCHITECTURE-MP.md`; what they should *feel like* lives here.

**Easy**: a friend who's playing for the first time. Builds slowly, sometimes forgets to train units, doesn't react well to harass. Win rate target vs intermediate player: ~10%.

**Medium**: a friend who knows the game but plays casually. Solid build order, sends units to defend, doesn't micro. Win rate target vs intermediate player: ~50%.

**Hard**: a friend who's been grinding. Tighter build order, larger army earlier, better targeting (focus-fire low HP units). **No resource cheats by default**. If Hard isn't beating an intermediate player ~80% of the time, give it small information advantages (e.g. omniscience through fog when fog exists) before resource cheats. Resource cheats break the simulation's honesty and players notice.

## Pacing targets (proposals)

- First unit on field: ~10s after match start (place Barracks → train Soldier).
- First skirmish: ~2-3 minutes.
- Critical mass for first push: ~5 minutes.
- Game-ending engagement: ~10-15 minutes for 1v1, ~20-25 for 4v4.

If the loop drags past these markers in playtest, tune unit cost / income up — not down.

## Anti-patterns / things to avoid

- **Multiple resources**. Earned right to add when the single-resource economy is provably boring.
- **Unit cap**. Solves a problem we don't have yet (performance). Add only if profiling demands it in Phase 5.
- **Hero units**. RTS hero design is hard; nothing here calls for it.
- **Tech tiers / upgrades**. One unit type per training building is enough through Phase 5. Upgrades are Phase 6+ if we have appetite.
- **Resource cheats on Hard bot**. (Repeated for emphasis.)
- **Asymmetric factions before symmetric balance works**. Symmetric is hard; asymmetric balance on top of broken symmetric is harder.

## Open design questions (revisit when relevant)

- Worker units (units that build) vs build-from-anywhere: current model has no workers; HQ/player builds buildings. Adding workers later is a real lift — flag if we want them.
- Population/supply cap: needed if armies grow unbounded. Defer until a playtest shows the problem.
- Bounties for kills / resource trickle from owned territory: depth options for later phases.
- "Rush" prevention: spawn-area protection, scout-only first minute, etc. Address in playtest.
