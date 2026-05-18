# CLAUDE.md — conventions & how to extend Conquerors

This document is read first by any future contributor (human or LLM) before changing the code. Read it end-to-end. Don't redocument what the code already says — only the things that surprised the last author go in here.

## Architecture in one paragraph

Three layers, top-down:
1. **Data** (`Conquerors.Data`) — pure records (no MonoGame deps). Loaded from JSON at startup. The only things in here are static definitions of game objects (e.g. `BuildingData`).
2. **Core / Entities / Systems** (`Conquerors.Core`, `Conquerors.Entities`, `Conquerors.Systems`) — gameplay state and logic. `World` holds mutable state; systems take a `World` by reference and mutate. Systems are plain classes with no static state; you pass dependencies in.
3. **Presentation** (`Conquerors.Rendering`, `Conquerors.UI`) — MonoGame-aware. Reads world state; never mutates it. Rendering is a function of state.

`Conquerors.Input` and `Conquerors.Persistence` sit alongside; both treat the world as data they read from / write to.

`GameRoot` (in `Core`) is the only class that wires everything together. It owns the camera, input, systems, and renderers, and runs the update/draw loop. Keep it slim — gameplay logic belongs in systems.

## Data flow per frame

```
Per-frame (runs at render rate, 60+Hz):
  Input.Poll                              (snapshot keyboard / mouse)
  ├─► CameraSystem.Update                 (mutates Camera2D from input)
  ├─► Camera2D.ClampTo                    (keep camera in world bounds)
  ├─► (build mode key handling)           (toggles PlacementSystem, picks defn)
  ├─► (LMB in build mode → Command)       (input translator enqueues PlaceBuildingCommand)
  ├─► Persistence keys (F5 / F9)          (serializer reads/writes World+ResSys)
  └─► SimClock.Advance(dt) → N            (returns whole sim ticks to run now)

For each of N ticks (sim, fixed 50ms dt):
  ├─► ResourceSystem.Update(world, 0.05)  (deterministic income tick)
  └─► CommandProcessor.ProcessAll         (drains buffer → systems mutate World)

Draw
  │ Begin SpriteBatch with camera transform
  ├─► GridRenderer.Draw                  (visible tiles only — view-culled)
  ├─► BuildingRenderer.Draw              (all placed buildings)
  └─► BuildingRenderer.DrawGhost         (only in build mode)
  │ End. Begin SpriteBatch (identity, screen space)
  └─► Hud.Draw                           (credits / FPS / build mode / hint)
```

`World` is the single source of truth. Tests construct one directly (no MonoGame needed) — see `TestWorlds`.

## PlayerId & entity ownership (Phase 2 prereq #4)

Every gameplay entity that can be owned is owned. `Building.Owner` is a `PlayerId`. The world holds a `Players` list of `Player(Id, Name, Team, Color)` records — humans and bots share this type; the sim doesn't distinguish them.

When applying a `PlaceBuildingCommand`, `PlacementSystem` copies `command.Issuer` onto the new `Building.Owner`. The save format round-trips both the player table and each building's owner.

**Not yet per-player:** `World.Credits` and `ResourceSystem`'s summed income remain global. Phase 2 has one human; the optional dummy player (no controller yet) builds nothing. Per-player economy comes when a second human player exists (P4 lockstep) or when bots start placing buildings (P3).

When the renderer becomes 3D, building/unit tint should sample `Player.Color`; the data is here, the renderer change is parked alongside the 3D pivot.

## Seeded RNG (Phase 2 prereq #3)

`World.Rng` (`MatchRng`) is the single source of gameplay randomness. It uses xorshift64* with a SplitMix64 seed finalizer — chosen for stability across .NET versions and architectures. **Never call `Random.Shared` or `new Random()` from sim code.** Saves persist `Rng.Seed` (audit) and `Rng.State` (resume), so reloading a mid-match save continues the RNG sequence exactly where it stopped.

Phase 2 doesn't draw from `Rng` yet; the plumbing is in place so Phase 3 (bot decisions) and later combat (random target tiebreaking, projectile spread if any) don't have to retrofit. Seed comes from the match config when one exists; the Phase 2 sandbox uses a constant (`GameRoot.SandboxSeed`).

## Sim tick (Phase 2 prereq #2)

Render runs at the host's framerate; the simulation advances in fixed 50ms steps (`SimClock.TicksPerSecond = 20`). `GameRoot.Update` calls `SimClock.Advance(dt)`, which returns how many tick steps to execute. Each step runs `ResourceSystem.Update` (with constant `SimClock.TickDt`) and `CommandProcessor.ProcessAll`. Camera/input/render stay per-frame.

Why fixed-step: peers in a lockstep MP match (Phase 4) must all step the same number of times with the same dt regardless of local framerate. Hard-coding this now means the gameplay logic we write in Phase 2/3 is already framerate-independent.

`Advance` caps catch-up at `DefaultMaxStepsPerAdvance` (5) to prevent a spiral of death after long stalls (debugger break, GPU hang). Residual time stays in the accumulator and is consumed on subsequent calls.

## Determinism discipline (Phase 2 prereq #5)

Lockstep MP (Phase 4) requires peers to compute identical world state from identical inputs. The full rule set lives in `docs/ARCHITECTURE-MP.md §Determinism`. The short list of things **forbidden in sim code** (`Commands/`, `Core/`, `Data/`, `Entities/`, `Systems/`):

- Transcendentals: `Math.{Sin,Cos,Tan,Atan,Atan2,Asin,Acos,Exp,Log,Pow}` and `MathF.*` — cross-CPU/runtime variance.
- Unseeded RNG: `Random.Shared`, `new Random(...)`. Use `World.Rng` (`MatchRng`).
- Wall clocks: `DateTime.Now`, `DateTime.UtcNow`, `Stopwatch`.
- Allocation-derived ids: `Guid.NewGuid`. Use `World.NextId()`.

`DeterminismDisciplineTests.Sim_Code_Contains_No_Forbidden_APIs` is the enforcement layer — a source scanner that walks the sim subdirs and fails the build on any forbidden token. It strips `//` and `/* */` comments and ignores `using` directives so the rule reads as "tokens in live code." `GameRoot.cs` is exempt (MonoGame wiring layer — `Stopwatch` powers the FPS counter, none of its state is sim state).

This is intentionally line-scanning, not Roslyn-analyser-based. A real analyser is the longer-term home, but the scanner is good enough today and ships zero infrastructure cost. To exempt a new wiring file, add its basename to `ExemptFiles` and justify the exemption in a comment.

Presentation code (`Rendering/`, `UI/`, `Input/`, `Persistence/`) is **not** scanned — it can use floats, clocks, system RNG freely. The rule is: anything that writes to `World` follows determinism discipline; anything that only reads `World` does not.

## Command stream (Phase 2 prereq #1)

Every gameplay-mutating action is a `Command` record. The input layer (GameRoot) translates clicks/keys into commands and enqueues them on a `CommandBuffer`; `CommandProcessor.ProcessAll` drains the buffer each tick and dispatches to the owning system. **Systems never read input directly.** This is what makes lockstep MP and deterministic replays cheap in later phases — we can swap the input translator for a network-decoded bundle without touching the sim.

`BuildMode` / `SelectedDefinitionId` on `PlacementSystem` are UI state — they gate command *emission*, not command *application*. `Apply(World, PlaceBuildingCommand)` validates from the command's own fields and is independent of any UI flag.

When adding a new gameplay action: define a new `…Command` record under `Commands/`, add a case to `CommandProcessor.Apply`, and route the input that triggers it through `CommandBuffer.Enqueue`. Never call a mutating system method directly from `GameRoot.Update`.

## How to add a new building type

1. **Define it** in `assets/data/buildings.json`. All fields are required:
   ```json
   { "Id": "turret", "Name": "Turret", "Cost": 150, "Width": 1, "Height": 1,
     "CreditsPerSecond": 0.0, "Color": { "R": 200, "G": 200, "B": 80 } }
   ```
2. **Make it placeable**: add its id to `GameRoot`'s `_placementSystem` build order:
   ```csharp
   new PlacementSystem(new[] { "collector", "barracks", "turret" })
   ```
   And add a hotkey case (e.g. `Keys.D3`) in `UpdatePlacement()`.
3. **(Optional) Behaviour**: if it's not a pure resource generator, add a new system that scans `world.Buildings` for `DefinitionId == "turret"` and acts on them. Put the system in `Systems/`, give it an `Update(World, float dt)` method, and call it from `GameRoot.Update`.
4. **Tests**: add a fact in `BuildingCatalogTests` if it needs special validation, and one in `PlacementSystemTests` if its placement rules differ.

Definitions are referenced by string id so save files survive renames-as-additions (rename = breaking change; treat as a save migration).

## How to add a new system

1. New file under `src/Conquerors.Game/Systems/`, e.g. `CombatSystem.cs`.
2. Plain class, no statics, no `Game` base. Single `Update(World world, float dt, …)` entry point. Read what you need from `World`; mutate what you own; don't reach into `GraphicsDevice` or `Content`.
3. Wire it in `GameRoot`: instantiate as a `private readonly` field, call `system.Update(_world, dt, …)` from `Update`.
4. Write a test: construct a `World` with `TestWorlds.Fresh()`, exercise the system, assert on world state. Tests must not touch MonoGame.

## Conventions (don't re-derive from EditorConfig)

- C# 12 (records, primary constructors, target-typed `new`). `.NET 8`.
- File-scoped namespaces. One public type per file unless tightly coupled (`SaveData` + `BuildingSave`, `BuildingData` + `ColorRgb`).
- Nullable reference types are on; warnings are errors. Treat any warning as a real failure — don't suppress.
- `var` for obvious types only; never for primitives.
- XML doc comments on public APIs only. No prose comments on private helpers; clear names instead.
- Pass dependencies explicitly. No service locator, no DI container. If wiring gets painful, the cure is composition in `GameRoot`, not a container.
- No statics except genuinely stateless helpers (e.g. `SavePaths`).

## Data-driven discipline

Gameplay values (costs, sizes, income, colours) live in JSON — never as literals in systems. If a system needs a tuned constant, ask whether it belongs in `BuildingData` or a new `…Data` record loaded from JSON. Systems should read; not embed.

## Apple Silicon notes (MonoGame 3.8.4)

- `.NET 8` SDK installed via `brew install dotnet@8` is keg-only; export `DOTNET_ROOT` and add `$DOTNET_ROOT/../bin` to `PATH` (see README).
- MGCB SpriteFont compilation **works** on macOS arm64 with MonoGame.Content.Builder.Task 3.8.4. It resolves system fonts via `/System/Library/Fonts/Supplemental/*.ttf`. If a font name doesn't resolve, prefer `Arial`, `Menlo`, or `Helvetica` (always present on macOS) or ship a TTF in `Content/` and reference it from the `.spritefont` file.
- MGCB Effect (shader) compilation has historically required Wine on arm64. Phase 1 ships no custom shaders, so this isn't exercised. If you add one, expect to install `mgfxc` via the documented MonoGame Effect Compiler workflow, or wait until MG ships a native arm64 path.
- If MGCB ever silently picks the wrong asset (e.g. capitalisation differs from the path), prefix the asset path in `.mgcb` exactly as it appears on disk — MGCB is case-sensitive across platforms.

## Current limitations (Phase 1)

- Only one save slot. F5 overwrites; F9 loads. No autosave, no save list UI.
- No camera momentum, no smoothing, no zoom-to-cursor. Zoom centres on the viewport.
- Edge scrolling pans even when the cursor sits exactly on an edge pixel — it's gated only on window focus.
- Buildings have no orientation, no upgrade path, no demolition.
- No selection of placed buildings; "currently selected building info" in the HUD refers to the build-mode-selected definition (since there's nothing to select yet).
- Resource ticks are real-time and dependent on wall-clock dt. With long stalls (debugger break, GPU hang) you may briefly fall behind.
- `World.AddBuilding` throws on collisions but `PlacementSystem.TryPlace` checks first — keep them aligned if you bypass the system. Tests cover the happy path; the throw path is a safety net.
- No tests against the GameRoot integration (it spins up MonoGame). Systems are tested directly.

## Out of scope (Phase 2+ — don't sneak in)

Units, pathfinding, combat, AI opponent, networking, procedural maps, art, audio. If you touch any of these in a Phase 1 PR, split it.
