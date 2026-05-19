using Conquerors.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Conquerors.Rendering;

/// <summary>
/// Drives the <see cref="RtsCamera3D"/> from input:
/// - WASD pans the target on the ground plane, aligned to the camera's yaw
///   (so "forward" is whatever direction the camera is facing).
/// - Edge-of-screen scrolling (toggle: F2) pans the same way.
/// - RMB drag rotates yaw (horizontal) and pitch (vertical).
/// - Scroll wheel zooms via distance.
/// Lives in <c>Rendering/</c>, not <c>Systems/</c>, because the camera is
/// presentation — it never writes to <c>World</c>.
/// </summary>
public sealed class RtsCameraController
{
    public bool EdgeScrollEnabled { get; private set; } = true;
    public float PanSpeed { get; init; } = 18f;          // tiles/sec at default distance
    public float EdgeScrollSpeed { get; init; } = 16f;
    public float EdgeScrollMargin { get; init; } = 24f;
    public float ZoomStep { get; init; } = 0.12f;
    public float YawSensitivity { get; init; } = 0.006f;
    public float PitchSensitivity { get; init; } = 0.005f;

    public void Update(RtsCamera3D camera, InputManager input, float dt, bool windowFocused)
    {
        if (input.WasKeyPressed(Keys.F2))
        {
            EdgeScrollEnabled = !EdgeScrollEnabled;
        }

        if (input.RightDown)
        {
            Point md = input.MouseDelta;
            camera.Yaw -= md.X * YawSensitivity;
            camera.Pitch = System.Math.Clamp(
                camera.Pitch + md.Y * PitchSensitivity,
                camera.MinPitch,
                camera.MaxPitch);
        }

        if (input.ScrollDelta != 0)
        {
            float factor = input.ScrollDelta > 0 ? 1f / (1f + ZoomStep) : 1f + ZoomStep;
            camera.Distance = System.Math.Clamp(
                camera.Distance * factor,
                camera.MinDistance,
                camera.MaxDistance);
        }

        // Build a yaw-relative ground-plane basis so pan directions feel right.
        float cy = (float)System.Math.Cos(camera.Yaw);
        float sy = (float)System.Math.Sin(camera.Yaw);
        Vector3 forward = new(-sy, 0f, -cy);   // "away from camera" along ground
        Vector3 right = new(cy, 0f, -sy);

        Vector3 dir = Vector3.Zero;
        if (input.IsKeyDown(Keys.W)) dir += forward;
        if (input.IsKeyDown(Keys.S)) dir -= forward;
        if (input.IsKeyDown(Keys.A)) dir -= right;
        if (input.IsKeyDown(Keys.D)) dir += right;
        if (dir.LengthSquared() > 0f)
        {
            dir.Normalize();
            camera.Target += dir * PanSpeed * dt;
        }

        if (EdgeScrollEnabled && windowFocused && !input.RightDown)
        {
            Point mp = input.MousePosition;
            Vector3 edge = Vector3.Zero;
            if (mp.X >= 0 && mp.X < EdgeScrollMargin) edge -= right;
            else if (mp.X > camera.ViewportWidth - EdgeScrollMargin && mp.X <= camera.ViewportWidth) edge += right;
            if (mp.Y >= 0 && mp.Y < EdgeScrollMargin) edge += forward;
            else if (mp.Y > camera.ViewportHeight - EdgeScrollMargin && mp.Y <= camera.ViewportHeight) edge -= forward;
            if (edge.LengthSquared() > 0f)
            {
                edge.Normalize();
                camera.Target += edge * EdgeScrollSpeed * dt;
            }
        }
    }
}
