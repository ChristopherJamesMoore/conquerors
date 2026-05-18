namespace Conquerors.Core;

/// <summary>
/// Identifies a team within a match. Independent of <see cref="PlayerId"/>: 5v5 has
/// two teams of five, 4-player FFA has four teams of one. Phase 2 only uses
/// <see cref="Solo"/>; teams matter in P4+.
/// </summary>
public readonly record struct TeamId(int Value)
{
    public static TeamId Solo => new(0);
}
