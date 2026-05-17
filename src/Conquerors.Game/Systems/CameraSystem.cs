using Conquerors.Input;
using Conquerors.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Conquerors.Systems;

/// <summary>
/// Drives the camera from input: WASD pan, mouse-wheel zoom, edge-of-screen scrolling.
/// F2 toggles edge scrolling. Default ON.
/// </summary>
public sealed class CameraSystem
{
    public bool EdgeScrollEnabled { get; private set; } = true;
    public float PanSpeed { get; init; } = 600f;
    public float EdgeScrollSpeed { get; init; } = 500f;
    public float EdgeScrollMargin { get; init; } = 24f;
    public float ZoomStep { get; init; } = 0.12f;

    public void Update(Camera2D camera, InputManager input, float dt, bool windowFocused)
    {
        if (input.WasKeyPressed(Keys.F2))
        {
            EdgeScrollEnabled = !EdgeScrollEnabled;
        }

        Vector2 dir = Vector2.Zero;
        if (input.IsKeyDown(Keys.W)) dir.Y -= 1;
        if (input.IsKeyDown(Keys.S)) dir.Y += 1;
        if (input.IsKeyDown(Keys.A)) dir.X -= 1;
        if (input.IsKeyDown(Keys.D)) dir.X += 1;
        if (dir != Vector2.Zero)
        {
            dir.Normalize();
            camera.Position += dir * (PanSpeed / camera.Zoom) * dt;
        }

        if (EdgeScrollEnabled && windowFocused)
        {
            Point mp = input.MousePosition;
            Vector2 edge = Vector2.Zero;
            if (mp.X >= 0 && mp.X < EdgeScrollMargin) edge.X -= 1;
            else if (mp.X > camera.ViewportWidth - EdgeScrollMargin && mp.X <= camera.ViewportWidth) edge.X += 1;
            if (mp.Y >= 0 && mp.Y < EdgeScrollMargin) edge.Y -= 1;
            else if (mp.Y > camera.ViewportHeight - EdgeScrollMargin && mp.Y <= camera.ViewportHeight) edge.Y += 1;
            if (edge != Vector2.Zero)
            {
                edge.Normalize();
                camera.Position += edge * (EdgeScrollSpeed / camera.Zoom) * dt;
            }
        }

        if (input.ScrollDelta != 0)
        {
            float factor = input.ScrollDelta > 0 ? 1f + ZoomStep : 1f / (1f + ZoomStep);
            camera.Zoom = System.Math.Clamp(camera.Zoom * factor, camera.MinZoom, camera.MaxZoom);
        }
    }
}
