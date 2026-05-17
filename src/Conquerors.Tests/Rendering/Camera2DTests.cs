using Conquerors.Rendering;
using Microsoft.Xna.Framework;

namespace Conquerors.Tests.Rendering;

public class Camera2DTests
{
    [Fact]
    public void Default_Zoom_IsOne()
    {
        Camera2D c = new();
        Assert.Equal(1f, c.Zoom);
    }

    [Fact]
    public void ScreenToWorld_AtViewportCentre_ReturnsCameraPosition()
    {
        Camera2D c = new() { Position = new Vector2(500, 300), ViewportWidth = 1280, ViewportHeight = 720 };
        Vector2 centre = new(640, 360);
        Vector2 w = c.ScreenToWorld(centre);
        Assert.InRange((w - c.Position).Length(), 0, 0.0001f);
    }

    [Fact]
    public void ScreenToWorld_AndBack_RoundTrips()
    {
        Camera2D c = new() { Position = new Vector2(100, 200), Zoom = 1.5f, ViewportWidth = 800, ViewportHeight = 600 };
        Vector2 screen = new(123, 456);
        Vector2 w = c.ScreenToWorld(screen);
        Vector2 back = c.WorldToScreen(w);
        Assert.InRange((back - screen).Length(), 0, 0.01f);
    }

    [Fact]
    public void ClampTo_ConstrainsPosition()
    {
        Camera2D c = new() { Position = new Vector2(-100, 9999), ViewportWidth = 800, ViewportHeight = 600 };
        c.ClampTo(new Rectangle(0, 0, 1000, 1000));
        Assert.Equal(0, c.Position.X);
        Assert.Equal(1000, c.Position.Y);
    }
}
