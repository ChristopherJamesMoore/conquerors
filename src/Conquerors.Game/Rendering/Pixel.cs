using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Conquerors.Rendering;

/// <summary>
/// A 1x1 white texture used as a generic quad source. Renderers tint it
/// to draw arbitrary coloured rectangles without any art assets.
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
