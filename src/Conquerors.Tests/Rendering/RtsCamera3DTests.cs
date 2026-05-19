using Conquerors.Rendering;
using Microsoft.Xna.Framework;

namespace Conquerors.Tests.Rendering;

public class RtsCamera3DTests
{
    [Fact]
    public void Position_OrbitsTarget_AtDistance()
    {
        RtsCamera3D c = new()
        {
            Target = new Vector3(10, 0, 10),
            Yaw = 0f,
            Pitch = 0f,
            Distance = 5f,
        };
        Vector3 expected = c.Target + new Vector3(0, 0, 5);
        Assert.InRange((c.Position - expected).Length(), 0f, 1e-4f);
    }

    [Fact]
    public void ScreenToGround_AtViewportCentre_HitsNearTarget()
    {
        RtsCamera3D c = new()
        {
            Target = new Vector3(32, 0, 32),
            ViewportWidth = 1280,
            ViewportHeight = 720,
        };
        Assert.True(c.ScreenToGround(new Point(640, 360), out Vector3 hit));
        // Centre pixel + camera-on-target → ray should hit the ground near the target.
        Assert.InRange((new Vector2(hit.X, hit.Z) - new Vector2(c.Target.X, c.Target.Z)).Length(), 0f, 1f);
    }

    [Fact]
    public void ScreenToGround_ReturnsFalse_For_Sky_Pointing_Pixel()
    {
        RtsCamera3D c = new()
        {
            Target = new Vector3(32, 0, 32),
            Pitch = MathHelper.ToRadians(20f),
            ViewportWidth = 1280,
            ViewportHeight = 720,
        };
        // A pixel at the very top of the screen looks at the horizon / sky.
        Assert.False(c.ScreenToGround(new Point(640, 0), out _));
    }

    [Fact]
    public void ClampTargetTo_ConstrainsX_And_Z_Only()
    {
        RtsCamera3D c = new() { Target = new Vector3(-10f, 5f, 9999f) };
        c.ClampTargetTo(0f, 0f, 64f, 64f);
        Assert.Equal(0f, c.Target.X);
        Assert.Equal(5f, c.Target.Y);
        Assert.Equal(64f, c.Target.Z);
    }
}
