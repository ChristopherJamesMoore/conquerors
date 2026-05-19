using Conquerors.Core;
using Conquerors.Data;
using Conquerors.Entities;
using Microsoft.Xna.Framework;

namespace Conquerors.Rendering;

/// <summary>
/// Renders buildings as coloured cubes whose footprint matches <c>BuildingData</c>
/// and whose height comes from a small lookup (HQ is taller than a collector).
/// </summary>
public sealed class BuildingRenderer3D
{
    public void Draw(PrimitiveRenderer prim, World world)
    {
        foreach (Building b in world.Buildings)
        {
            BuildingData def = world.Catalog.Get(b.DefinitionId);
            Matrix world_ = WorldMatrix(b.Tile, def.Width, def.Height, HeightOf(def.Id));
            prim.DrawCube(world_, ToColor(def.Color));
        }
    }

    public void DrawGhost(PrimitiveRenderer prim, World world, TileCoord tile, string definitionId, bool valid)
    {
        BuildingData def = world.Catalog.Get(definitionId);
        Matrix world_ = WorldMatrix(tile, def.Width, def.Height, HeightOf(def.Id));
        Color tint = valid
            ? new Color(ToColor(def.Color).R, ToColor(def.Color).G, ToColor(def.Color).B, (byte)130)
            : new Color((byte)255, (byte)80, (byte)80, (byte)140);
        prim.DrawCube(world_, tint);
    }

    private static Matrix WorldMatrix(TileCoord tile, int wTiles, int hTiles, float height)
    {
        return Matrix.CreateScale(wTiles, height, hTiles)
             * Matrix.CreateTranslation(tile.X + wTiles * 0.5f, height * 0.5f, tile.Y + hTiles * 0.5f);
    }

    private static float HeightOf(string id) => id switch
    {
        "hq" => 3.5f,
        "barracks" => 2.2f,
        "collector" => 1.4f,
        _ => 1.5f,
    };

    private static Color ToColor(ColorRgb c) => new(c.R, c.G, c.B);
}
