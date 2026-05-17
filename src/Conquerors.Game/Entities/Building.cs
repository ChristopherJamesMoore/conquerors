using Conquerors.Core;
using Conquerors.Data;

namespace Conquerors.Entities;

/// <summary>
/// A placed building instance. References a BuildingData definition by id.
/// Tile is the top-left of the footprint; size comes from the definition.
/// </summary>
public sealed record Building(int Id, string DefinitionId, TileCoord Tile)
{
    public RectInt Footprint(BuildingCatalog catalog)
    {
        BuildingData def = catalog.Get(DefinitionId);
        return new RectInt(Tile.X, Tile.Y, def.Width, def.Height);
    }
}
