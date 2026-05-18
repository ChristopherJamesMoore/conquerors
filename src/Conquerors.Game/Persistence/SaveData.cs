using System.Collections.Generic;
using Conquerors.Data;

namespace Conquerors.Persistence;

/// <summary>Serialised world snapshot. Versioned so we can refuse incompatible saves.</summary>
public sealed record SaveData(
    int Version,
    int Credits,
    double ResourceCarry,
    int NextEntityId,
    List<BuildingSave> Buildings,
    ulong RngSeed = 0,
    ulong RngState = 0,
    List<PlayerSave>? Players = null);

/// <summary>Serialised building. References a catalog definition by id.</summary>
public sealed record BuildingSave(int Id, string DefinitionId, int X, int Y, int Owner = 0);

/// <summary>Serialised player record. Owner ids on buildings/units reference these.</summary>
public sealed record PlayerSave(int Id, string Name, int Team, ColorRgb Color);
