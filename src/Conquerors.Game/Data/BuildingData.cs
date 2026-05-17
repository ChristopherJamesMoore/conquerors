namespace Conquerors.Data;

/// <summary>
/// Static definition of a building type. Loaded from JSON; pure data, no MG dependency.
/// </summary>
public sealed record BuildingData(
    string Id,
    string Name,
    int Cost,
    int Width,
    int Height,
    float CreditsPerSecond,
    ColorRgb Color);

/// <summary>RGB in 0-255 — kept independent of MonoGame's Color to keep Data pure.</summary>
public readonly record struct ColorRgb(byte R, byte G, byte B);
