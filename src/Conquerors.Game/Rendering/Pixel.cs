using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Conquerors.Rendering;

/// <summary>
/// A 1x1 white texture used by screen-space (SpriteBatch) HUD elements.
/// The 3D scene doesn't need it — meshes carry their own colour.
/// </summary>
public sealed class Pixel : System.IDisposable
{
    public Texture2D Texture { get; }

    public Pixel(GraphicsDevice gd)
    {
        Texture = new Texture2D(gd, 1, 1);
        Texture.SetData([Color.White]);
    }

    public void Dispose() => Texture.Dispose();
}
