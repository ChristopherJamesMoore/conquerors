using Conquerors.Data;

namespace Conquerors.Core;

/// <summary>
/// Per-match player record. Humans and bots share this type. <see cref="Color"/>
/// is the team-tint colour used for rendering owned units and buildings (P2 keeps
/// the renderer untouched; the data is here for when it switches).
/// </summary>
public sealed record Player(PlayerId Id, string Name, TeamId Team, ColorRgb Color);
