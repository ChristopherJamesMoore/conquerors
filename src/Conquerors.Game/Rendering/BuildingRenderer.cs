using Conquerors.Core;
using Conquerors.Data;
using Conquerors.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Conquerors.Rendering;

/// <summary>
/// Draws placed buildings (and optionally a ghost) as coloured quads using the 1x1 pixel.
/// </summary>
public sealed class BuildingRenderer
{
    private readonly Texture2D _pixel;

    public BuildingRenderer(Texture2D pixel)
    {
        _pixel = pixel;
    }

    public void Draw(SpriteBatch sb, World world)
    {
        foreach (Building b in world.Buildings)
        {
            BuildingData def = world.Catalog.Get(b.DefinitionId);
            Rectangle r = TileRect(world.Grid, b.Tile, def.Width, def.Height);
            sb.Draw(_pixel, r, ToColor(def.Color));
            DrawBorder(sb, r, thickness: 2, new Color(0, 0, 0, 140));
        }
    }

    public void DrawGhost(SpriteBatch sb, World world, TileCoord tile, string definitionId, bool valid)
    {
        BuildingData def = world.Catalog.Get(definitionId);
        Rectangle r = TileRect(world.Grid, tile, def.Width, def.Height);
        Color body = ToColor(def.Color) * 0.45f;
        Color border = valid ? new Color(255, 255, 255, 180) : new Color(255, 80, 80, 200);
        sb.Draw(_pixel, r, body);
        DrawBorder(sb, r, thickness: 2, border);
    }

    private static Rectangle TileRect(Grid grid, TileCoord tile, int wTiles, int hTiles) =>
        new(tile.X * grid.TileSize, tile.Y * grid.TileSize, wTiles * grid.TileSize, hTiles * grid.TileSize);

    private static Color ToColor(ColorRgb c) => new(c.R, c.G, c.B);

    private void DrawBorder(SpriteBatch sb, Rectangle r, int thickness, Color color)
    {
        sb.Draw(_pixel, new Rectangle(r.X, r.Y, r.Width, thickness), color);
        sb.Draw(_pixel, new Rectangle(r.X, r.Bottom - thickness, r.Width, thickness), color);
        sb.Draw(_pixel, new Rectangle(r.X, r.Y, thickness, r.Height), color);
        sb.Draw(_pixel, new Rectangle(r.Right - thickness, r.Y, thickness, r.Height), color);
    }
}
