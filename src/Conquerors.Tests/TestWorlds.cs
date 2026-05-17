using Conquerors.Core;
using Conquerors.Data;

namespace Conquerors.Tests;

/// <summary>Shared test fixtures: a small known catalog and a fresh world.</summary>
internal static class TestWorlds
{
    public static BuildingCatalog Catalog() => new(new[]
    {
        new BuildingData("hq", "HQ", Cost: 0, Width: 3, Height: 3, CreditsPerSecond: 1.0f, new ColorRgb(235, 180, 90)),
        new BuildingData("collector", "Collector", Cost: 100, Width: 2, Height: 2, CreditsPerSecond: 2.0f, new ColorRgb(90, 200, 140)),
        new BuildingData("barracks", "Barracks", Cost: 200, Width: 3, Height: 2, CreditsPerSecond: 0.0f, new ColorRgb(200, 80, 80)),
    });

    public static World Fresh(int credits = 500, int gridSize = 16, ulong seed = 0xDEADBEEFUL)
    {
        Grid grid = new(gridSize, gridSize, tileSize: 32);
        return new World(grid, Catalog(), credits, seed);
    }
}
