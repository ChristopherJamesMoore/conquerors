using System.Collections.Generic;
using Conquerors.Commands;
using Conquerors.Core;
using Conquerors.Data;
using Conquerors.Entities;

namespace Conquerors.Systems;

/// <summary>Result of a placement check. <see cref="Ok"/> means a place would succeed.</summary>
public enum PlacementResult
{
    Ok,
    OffMap,
    Occupied,
    InsufficientCredits,
}

/// <summary>
/// Build-mode UI state plus the validator/applier for <see cref="PlaceBuildingCommand"/>.
/// Build mode and the current selection are local-input concerns (they drive the ghost
/// preview and the build-menu hotkeys); the command path is independent of them.
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
    public PlacementResult Check(World world, string definitionId, TileCoord tile)
    {
        BuildingData def = world.Catalog.Get(definitionId);
        RectInt fp = new(tile.X, tile.Y, def.Width, def.Height);
        if (!world.Grid.IsInside(fp)) return PlacementResult.OffMap;
        if (!world.Grid.CanPlace(fp)) return PlacementResult.Occupied;
        if (world.Credits < def.Cost) return PlacementResult.InsufficientCredits;
        return PlacementResult.Ok;
    }

    /// <summary>
    /// Apply a place-building command: validate, then on Ok deduct cost and add the
    /// building. Does NOT consult <see cref="BuildMode"/> — that's a UI affordance,
    /// not a sim invariant.
    /// </summary>
    public PlacementResult Apply(World world, PlaceBuildingCommand command)
    {
        PlacementResult check = Check(world, command.DefinitionId, command.Tile);
        if (check != PlacementResult.Ok)
        {
            return check;
        }
        BuildingData def = world.Catalog.Get(command.DefinitionId);
        world.Credits -= def.Cost;
        world.AddBuilding(new Building(world.NextId(), def.Id, command.Tile, command.Issuer));
        return PlacementResult.Ok;
    }
}
