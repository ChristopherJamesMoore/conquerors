namespace Conquerors.Core;

/// <summary>
/// Identifies a player in a match. Humans and bots share this type; the simulation
/// does not distinguish them. Phase 2 has one local human; prereq #4 fleshes this
/// out into a real per-match player table supporting up to 10.
/// </summary>
public readonly record struct PlayerId(int Value)
{
    /// <summary>The single local human player used during Phase 2 sandbox sessions.</summary>
    public static PlayerId Local => new(0);
}
