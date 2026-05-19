using Conquerors.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Conquerors.Rendering;

/// <summary>
/// Draws the world's grid as a ground plane (Y=0) with darker grid lines.
/// One world unit = one tile, so a 64×64 grid spans 0..64 on X/Z.
/// </summary>
public sealed class GridRenderer3D
{
    public Color GroundColor { get; init; } = new(40, 48, 60);
    public Color LineColor { get; init; } = new(72, 84, 100);
    public Color BorderColor { get; init; } = new(220, 220, 240);

    public void Draw(PrimitiveRenderer prim, Grid grid)
    {
        prim.DrawGroundQuad(0f, 0f, grid.Width, grid.Height, GroundColor);

        int lineCount = (grid.Width + 1) + (grid.Height + 1);
        VertexPositionColor[] lines = new VertexPositionColor[lineCount * 2];
        const float y = 0.001f;
        int i = 0;
        for (int x = 0; x <= grid.Width; x++)
        {
            Color c = (x == 0 || x == grid.Width) ? BorderColor : LineColor;
            lines[i++] = new VertexPositionColor(new Vector3(x, y, 0), c);
            lines[i++] = new VertexPositionColor(new Vector3(x, y, grid.Height), c);
        }
        for (int z = 0; z <= grid.Height; z++)
        {
            Color c = (z == 0 || z == grid.Height) ? BorderColor : LineColor;
            lines[i++] = new VertexPositionColor(new Vector3(0, y, z), c);
            lines[i++] = new VertexPositionColor(new Vector3(grid.Width, y, z), c);
        }
        prim.DrawLines(lines);
    }
}
