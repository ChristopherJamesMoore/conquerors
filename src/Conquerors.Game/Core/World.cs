using System.Collections.Generic;
using Conquerors.Data;
using Conquerors.Entities;

namespace Conquerors.Core;

/// <summary>
/// Mutable container of all gameplay state: grid, buildings, credits.
/// No update/draw logic — systems take a World by reference and mutate it.
/// </summary>
public sealed class World
{
    public Grid Grid { get; }
    public BuildingCatalog Catalog { get; }
    public List<Building> Buildings { get; }
    public int Credits { get; set; }

    private int _nextEntityId;

    public World(Grid grid, BuildingCatalog catalog, int startingCredits)
    {
        Grid = grid;
        Catalog = catalog;
        Buildings = new List<Building>();
        Credits = startingCredits;
        _nextEntityId = 1;
    }

    public int NextId() => _nextEntityId++;

    public int PeekNextId() => _nextEntityId;

    /// <summary>Used by the persistence layer when restoring saved state.</summary>
    public void SetNextId(int value) => _nextEntityId = value;

    /// <summary>
    /// Add a fully-formed building, marking its footprint occupied. Throws if the
    /// footprint is already taken or out of bounds.
    /// </summary>
    public void AddBuilding(Building b)
    {
        RectInt fp = b.Footprint(Catalog);
        Grid.Occupy(fp);
        Buildings.Add(b);
        if (b.Id >= _nextEntityId)
        {
            _nextEntityId = b.Id + 1;
        }
    }
}
