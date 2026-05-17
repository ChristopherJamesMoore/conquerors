using Microsoft.Xna.Framework;

namespace Conquerors.Rendering;

/// <summary>
/// 2D camera: world-space position (camera centre), uniform zoom, viewport size.
/// Pure data + matrix math. The CameraSystem is what mutates it from input.
/// </summary>
public sealed class Camera2D
{
    public Vector2 Position { get; set; }
    public float Zoom { get; set; } = 1f;
    public float MinZoom { get; set; } = 0.4f;
    public float MaxZoom { get; set; } = 3.0f;
    public int ViewportWidth { get; set; }
    public int ViewportHeight { get; set; }

    public Matrix GetViewMatrix() =>
        Matrix.CreateTranslation(-Position.X, -Position.Y, 0f)
        * Matrix.CreateScale(Zoom, Zoom, 1f)
        * Matrix.CreateTranslation(ViewportWidth * 0.5f, ViewportHeight * 0.5f, 0f);

    public Vector2 ScreenToWorld(Vector2 screen) =>
        Vector2.Transform(screen, Matrix.Invert(GetViewMatrix()));

    public Vector2 WorldToScreen(Vector2 world) =>
        Vector2.Transform(world, GetViewMatrix());

    /// <summary>Clamp camera centre into a rectangle (typically world pixel bounds).</summary>
    public void ClampTo(Rectangle worldPixelBounds)
    {
        float x = System.Math.Clamp(Position.X, worldPixelBounds.Left, worldPixelBounds.Right);
        float y = System.Math.Clamp(Position.Y, worldPixelBounds.Top, worldPixelBounds.Bottom);
        Position = new Vector2(x, y);
    }
}
