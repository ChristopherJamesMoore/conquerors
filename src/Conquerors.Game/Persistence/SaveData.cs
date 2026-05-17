using System.Collections.Generic;

namespace Conquerors.Persistence;

/// <summary>Serialised world snapshot. Versioned so we can refuse incompatible saves.</summary>
public sealed record SaveData(
    int Version,
    int Credits,
    double ResourceCarry,
    int NextEntityId,
    List<BuildingSave> Buildings);

/// <summary>Serialised building. References a catalog definition by id.</summary>
public sealed record BuildingSave(int Id, string DefinitionId, int X, int Y);
