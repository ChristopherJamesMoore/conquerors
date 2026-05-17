using Conquerors.Core;

namespace Conquerors.Commands;

/// <summary>
/// Base for every gameplay-mutating intent. Systems consume commands; nothing else
/// is allowed to mutate <see cref="World"/>. Records so equality is structural —
/// useful for replay diffing and determinism harness checks.
/// </summary>
public abstract record Command(PlayerId Issuer);

/// <summary>Place a building of <paramref name="DefinitionId"/> at <paramref name="Tile"/>.</summary>
public sealed record PlaceBuildingCommand(PlayerId Issuer, string DefinitionId, TileCoord Tile)
    : Command(Issuer);
