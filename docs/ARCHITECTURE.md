# ARCHITECTURE.md — modules and dependencies

This is the structural map. For conventions and how-to-extend, read `CLAUDE.md` first.

## Module dependency graph

```
                        ┌────────────────┐
                        │   Conquerors   │  ← GameRoot wires all
                        │     .Core      │     of these together
                        │  (GameRoot)    │
                        └────────────────┘
                          │   │  │  │  │  │
        ┌─────────────────┘   │  │  │  │  └────────────────┐
        ▼                     ▼  ▼  ▼  ▼                   ▼
   ┌────────┐     ┌─────────────────────┐     ┌──────────────────┐
   │ Input  │     │     Rendering       │     │     UI (HUD)     │
   │ (poll) │     │ Camera2D, Pixel,    │     │ Font + panels    │
   └────────┘     │ Grid/Building rend. │     └──────────────────┘
        │         └─────────────────────┘
        │                  │
        │                  ▼ reads
        │         ┌─────────────────────┐
        │         │       Systems       │
        │         │ Camera, Resource,   │ ──── mutate ───► World
        │         │ Placement           │
        │         └─────────────────────┘
        │                  │
        │                  ▼ uses
        │         ┌─────────────────────┐
        │         │ Core (World, Grid,  │
        │         │ TileCoord, RectInt) │ + Entities (Building)
        │         └─────────────────────┘
        │                  │
        │                  ▼ reads
        │         ┌─────────────────────┐
        │         │  Data (definitions, │ ← JSON in assets/data
        │         │  BuildingCatalog)   │
        │         └─────────────────────┘
        │
        ▼
   ┌──────────────────────────────────────┐
   │ Persistence (SaveData, Serializer,   │ ← Application Support / AppData
   │ SavePaths)                           │
   └──────────────────────────────────────┘

   Dependency direction is one-way and ranked:
     Data  ←  Core/Entities  ←  Systems  ←  Rendering/UI  ←  GameRoot
     Persistence ↔ Core (reads + writes World; reads Catalog)
```

Rule: **lower layers never reference higher ones.** Data has no MonoGame import; Systems have no `SpriteBatch`; Rendering never mutates `World`.

## Module responsibilities

### `Conquerors.Data`
Pure record definitions. JSON-deserialised at startup. No MonoGame, no I/O beyond `BuildingCatalog.LoadFromJson` (which takes a path).
- `BuildingData(Id, Name, Cost, Width, Height, CreditsPerSecond, Color)`
- `ColorRgb(R, G, B)` — kept independent of MG's `Color` so Data stays portable.
- `BuildingCatalog` — read-only registry keyed by id.

### `Conquerors.Core`
The gameplay world and its primitives. No rendering. Minimal MG: uses `Vector2` for math.
- `TileCoord`, `RectInt` — integer-space value types.
- `Grid` — dimensions, tile size, occupancy bitmap; `CanPlace` / `Occupy` / `Free` / `Clear`.
- `World` — grid + catalog + buildings + credits + entity id counter.
- `GameRoot` — MonoGame `Game` subclass. Wiring + loop. The one class that pulls everything together.

### `Conquerors.Entities`
- `Building(Id, DefinitionId, Tile)` — record; footprint computed from catalog.

### `Conquerors.Systems`
Plain update logic. Take a `World` and inputs, mutate the world.
- `CameraSystem` — pan / zoom / edge-scroll; toggles edge scroll on F2.
- `ResourceSystem` — sum CreditsPerSecond across buildings; accumulate fractional credits across frames.
- `PlacementSystem` — build-mode state machine; `Check` (pure) + `TryPlace` (mutating).

### `Conquerors.Input`
- `InputManager` — keyboard + mouse snapshots with previous-frame deltas (`WasKeyPressed`, `LeftClicked`, `ScrollDelta`).

### `Conquerors.Rendering`
MG-aware. Read-only on world state.
- `Camera2D` — view matrix, screen↔world transforms, clamp.
- `Pixel` — 1×1 white `Texture2D` used as a tintable quad source.
- `GridRenderer` — view-culled checkerboard.
- `BuildingRenderer` — placed buildings + ghost preview.

### `Conquerors.UI`
- `FpsCounter` — windowed wall-clock fps.
- `Hud` — panel + hint strip.

### `Conquerors.Persistence`
- `SaveData`, `BuildingSave` — versioned snapshot records.
- `SavePaths` — platform user-data dir resolution.
- `WorldSerializer` — JSON save / load with validation.

### `Conquerors.Tests` (project)
xUnit. Each system has a tests file under a matching subfolder. Tests construct a `World` via `TestWorlds.Fresh()`; they don't spin up MonoGame.

## Lifecycle

```
Program.Main
  → new GameRoot()
      LoadContent:
        - new SpriteBatch
        - new Pixel (1×1 white)
        - new GridRenderer, BuildingRenderer, Hud
        - Content.Load<SpriteFont>("Default")
        - BuildingCatalog.LoadFromJson(assets/data/buildings.json)
        - new Grid(64,64,32)
        - new World(grid, catalog, 500 credits)
        - World.AddBuilding(HQ at centre)
        - camera.Position = grid centre
      Update (60Hz fixed):
        - Input.Poll
        - CameraSystem.Update + clamp
        - ResourceSystem.Update
        - UpdatePlacement (B / 1 / 2 / LMB / RMB)
        - UpdatePersistence (F5 / F9)
      Draw:
        - Clear
        - SpriteBatch.Begin(camera transform): grid → buildings → ghost
        - SpriteBatch.Begin(identity): HUD
```

## Where state lives

| State                         | Owner                           |
| ----------------------------- | ------------------------------- |
| Credits                       | `World.Credits`                 |
| Buildings list                | `World.Buildings`               |
| Grid occupancy bitmap         | `World.Grid` (private)          |
| Resource fractional carry     | `ResourceSystem._carry`         |
| Build mode + selected defn    | `PlacementSystem`               |
| Camera position + zoom        | `Camera2D`                      |
| Edge scroll on/off            | `CameraSystem.EdgeScrollEnabled`|
| FPS sampler window            | `FpsCounter`                    |
| Save path                     | `SavePaths` (computed)          |

Persistence saves: Credits, ResourceCarry, NextEntityId, Buildings. Camera state and edge-scroll toggle are session-only by design.
