using System.Collections.Generic;
using Conquerors.Core;
using Conquerors.Data;
using Conquerors.Entities;

namespace Conquerors.Systems;

/// <summary>Result of a placement check. <see cref="Ok"/> means a place would succeed.</summary>
public enum PlacementResult
{
    Ok,
    NotInBuildMode,
    OffMap,
    Occupied,
    InsufficientCredits,
}

/// <summary>
/// Build-mode state machine and placement validator. Holds no input or rendering
/// dependencies; callers drive it with TileCoord values and check/place results.
/// </summary>
public sealed class PlacementSystem
{
    public IReadOnlyList<string> BuildOrder { get; }
    public bool BuildMode { get; private set; }
    public string SelectedDefinitionId { get; private set; }

    public PlacementSystem(IReadOnlyList<string> buildOrder)
    {
        if (buildOrder.Count == 0)
        {
            throw new System.ArgumentException("build order is empty", nameof(buildOrder));
        }
        BuildOrder = buildOrder;
        SelectedDefinitionId = buildOrder[0];
    }

    public void EnterBuildMode()
    {
        BuildMode = true;
        SelectedDefinitionId = BuildOrder[0];
    }

    public void ExitBuildMode() => BuildMode = false;

    public void ToggleBuildMode()
    {
        if (BuildMode) ExitBuildMode();
        else EnterBuildMode();
    }

    public bool SelectByIndex(int index)
    {
        if (index < 0 || index >= BuildOrder.Count)
        {
            return false;
        }
        SelectedDefinitionId = BuildOrder[index];
        return true;
    }

    /// <summary>Pure check — does not mutate world. Returns the first failure reason or Ok.</summary>
    public PlacementResult Check(World world, TileCoord tile)
    {
        BuildingData def = world.Catalog.Get(SelectedDefinitionId);
        RectInt fp = new(tile.X, tile.Y, def.Width, def.Height);
        if (!world.Grid.IsInside(fp)) return PlacementResult.OffMap;
        if (!world.Grid.CanPlace(fp)) return PlacementResult.Occupied;
        if (world.Credits < def.Cost) return PlacementResult.InsufficientCredits;
        return PlacementResult.Ok;
    }

    /// <summary>Attempts to place; on Ok deducts cost and adds the building to the world.</summary>
    public bool TryPlace(World world, TileCoord tile, out PlacementResult result)
    {
        if (!BuildMode)
        {
            result = PlacementResult.NotInBuildMode;
            return false;
        }
        result = Check(world, tile);
        if (result != PlacementResult.Ok)
        {
            return false;
        }
        BuildingData def = world.Catalog.Get(SelectedDefinitionId);
        world.Credits -= def.Cost;
        world.AddBuilding(new Building(world.NextId(), def.Id, tile));
        return true;
    }
}
