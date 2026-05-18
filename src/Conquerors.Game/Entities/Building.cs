using Conquerors.Core;
using Conquerors.Data;

namespace Conquerors.Entities;

/// <summary>
/// A placed building instance. References a <see cref="BuildingData"/> definition
/// by id. <see cref="Tile"/> is the top-left of the footprint; size comes from the
/// definition. <see cref="Owner"/> identifies the player who controls it.
/// </summary>
public sealed record Building(int Id, string DefinitionId, TileCoord Tile, PlayerId Owner)
{
    public RectInt Footprint(BuildingCatalog catalog)
    {
        BuildingData def = catalog.Get(DefinitionId);
        return new RectInt(Tile.X, Tile.Y, def.Width, def.Height);
    }
}
