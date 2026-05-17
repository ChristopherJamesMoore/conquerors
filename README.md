# Conquerors

Top-down real-time strategy, base-building focused. Phase 1 prototype.

## Stack

- **.NET 8** + **C# 12**
- **MonoGame 3.8** (DesktopGL backend — runs natively on macOS Apple Silicon, macOS Intel, and Windows)
- **xUnit** for tests

## Prerequisites

- **macOS**: `.NET 8 SDK` — install via `brew install dotnet@8` (keg-only; export `DOTNET_ROOT` and add to `PATH` — see below)
- **Windows**: `.NET 8 SDK` from <https://dotnet.microsoft.com/download/dotnet/8.0>
- Optional: `git` for cloning

### macOS PATH setup (keg-only Homebrew install)

Add to `~/.zshrc` (or `~/.bash_profile`):

```sh
export DOTNET_ROOT="/opt/homebrew/opt/dotnet@8/libexec"
export PATH="/opt/homebrew/opt/dotnet@8/bin:$PATH"
```

Then `source ~/.zshrc` (or open a new terminal).

## Build & run

```sh
# Restore packages
dotnet restore

# Build
dotnet build

# Run the game
dotnet run --project src/Conquerors.Game

# Run tests
dotnet test
```

## Controls (Phase 1)

| Key | Action |
| --- | --- |
| `W A S D` | Pan camera |
| `Mouse wheel` | Zoom |
| `F2` | Toggle edge-of-screen scrolling (default ON) |
| `B` | Enter build mode |
| `1` / `2` | (In build mode) Select Collector / Barracks |
| `Left click` | Place building (in build mode) |
| `Right click` / `Esc` | Cancel build mode |
| `F5` | Save world |
| `F9` | Load world |

## Repository layout

```
Conquerors.sln
src/
  Conquerors.Game/      MonoGame DesktopGL app
    Core/               game loop, world, time
    Entities/           Entity, components (buildings)
    Systems/            update systems (resources, placement)
    Rendering/          Camera2D, GridRenderer, BuildingRenderer
    Input/              InputManager, keybindings
    Data/               BuildingData record + JSON catalog
    UI/                 HUD
    Persistence/        save/load
  Conquerors.Tests/     xUnit
assets/                 JSON data files
docs/                   architecture & conventions
```

See `docs/CLAUDE.md` for architecture, conventions, and how to extend (add a new building type, add a new system).

## Phase scope

Phase 1 (this prototype): window, camera, grid, resources, building placement, HUD, save/load.
Out of scope here: units, AI, networking, procedural maps, real art.
