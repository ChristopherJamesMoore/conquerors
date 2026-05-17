using Conquerors.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Conquerors.Rendering;

/// <summary>
/// Draws the tile grid as a coloured checkerboard, culling tiles outside the camera view.
/// Uses a single 1x1 pixel texture tinted per tile (no art assets).
/// </summary>
public sealed class GridRenderer
{
    private readonly Texture2D _pixel;

    public Color TileA { get; init; } = new(36, 42, 50);
    public Color TileB { get; init; } = new(44, 52, 64);
    public Color OutsideColor { get; init; } = new(12, 14, 18);

    public GridRenderer(Texture2D pixel)
    {
        _pixel = pixel;
    }

    public void Draw(SpriteBatch sb, Grid grid, Camera2D camera)
    {
        Vector2 tl = camera.ScreenToWorld(Vector2.Zero);
        Vector2 br = camera.ScreenToWorld(new Vector2(camera.ViewportWidth, camera.ViewportHeight));

        int x0 = System.Math.Max(0, (int)System.Math.Floor(tl.X / grid.TileSize));
        int y0 = System.Math.Max(0, (int)System.Math.Floor(tl.Y / grid.TileSize));
        int x1 = System.Math.Min(grid.Width - 1, (int)System.Math.Ceiling(br.X / grid.TileSize));
        int y1 = System.Math.Min(grid.Height - 1, (int)System.Math.Ceiling(br.Y / grid.TileSize));

        for (int x = x0; x <= x1; x++)
        {
            for (int y = y0; y <= y1; y++)
            {
                Color c = ((x + y) & 1) == 0 ? TileA : TileB;
                sb.Draw(_pixel, new Rectangle(x * grid.TileSize, y * grid.TileSize, grid.TileSize, grid.TileSize), c);
            }
        }
    }
}
