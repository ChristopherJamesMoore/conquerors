using Microsoft.Xna.Framework;

namespace Conquerors.Rendering;

/// <summary>
/// Roblox-style follow camera. Orbits a point on the ground (the "target")
/// at a configurable yaw, pitch and distance. The world is laid out with
/// +X east, +Z south, +Y up; the ground plane is Y=0. One world unit = one tile.
/// </summary>
/// <remarks>
/// All math here is presentation-only — calls to <c>Math.Sin/Cos</c> and float
/// arithmetic are fine because nothing this class produces is sim state.
/// </remarks>
public sealed class RtsCamera3D
{
    public Vector3 Target { get; set; }
    public float Yaw { get; set; } = 0f;
    public float Pitch { get; set; } = MathHelper.ToRadians(55f);
    public float Distance { get; set; } = 24f;

    public float MinDistance { get; init; } = 4f;
    public float MaxDistance { get; init; } = 80f;
    public float MinPitch { get; init; } = MathHelper.ToRadians(10f);
    public float MaxPitch { get; init; } = MathHelper.ToRadians(85f);

    public float FieldOfView { get; init; } = MathHelper.ToRadians(55f);
    public float NearPlane { get; init; } = 0.1f;
    public float FarPlane { get; init; } = 500f;

    public int ViewportWidth { get; set; }
    public int ViewportHeight { get; set; }

    public Vector3 Position
    {
        get
        {
            float cp = (float)System.Math.Cos(Pitch);
            float sp = (float)System.Math.Sin(Pitch);
            float cy = (float)System.Math.Cos(Yaw);
            float sy = (float)System.Math.Sin(Yaw);
            Vector3 offset = new(sy * cp, sp, cy * cp);
            return Target + offset * Distance;
        }
    }

    public Matrix View => Matrix.CreateLookAt(Position, Target, Vector3.Up);

    public Matrix Projection => Matrix.CreatePerspectiveFieldOfView(
        FieldOfView,
        ViewportHeight <= 0 ? 1f : (float)ViewportWidth / ViewportHeight,
        NearPlane,
        FarPlane);

    /// <summary>
    /// Project a screen-space pixel onto the world ground plane (Y=0).
    /// Returns false if the ray doesn't hit the plane (looking up at the sky).
    /// </summary>
    public bool ScreenToGround(Point screen, out Vector3 ground)
    {
        Matrix vp = View * Projection;
        Matrix inv = Matrix.Invert(vp);

        float nx = (2f * screen.X / System.Math.Max(1, ViewportWidth)) - 1f;
        float ny = 1f - (2f * screen.Y / System.Math.Max(1, ViewportHeight));

        Vector4 nearH = Vector4.Transform(new Vector4(nx, ny, 0f, 1f), inv);
        Vector4 farH = Vector4.Transform(new Vector4(nx, ny, 1f, 1f), inv);
        Vector3 near = new(nearH.X / nearH.W, nearH.Y / nearH.W, nearH.Z / nearH.W);
        Vector3 far = new(farH.X / farH.W, farH.Y / farH.W, farH.Z / farH.W);

        Vector3 dir = far - near;
        if (System.Math.Abs(dir.Y) < 1e-6f)
        {
            ground = default;
            return false;
        }
        float t = -near.Y / dir.Y;
        if (t < 0f || t > 1f)
        {
            ground = default;
            return false;
        }
        ground = near + dir * t;
        return true;
    }

    /// <summary>Clamp the camera target into a rectangle on the ground plane.</summary>
    public void ClampTargetTo(float minX, float minZ, float maxX, float maxZ)
    {
        Target = new Vector3(
            System.Math.Clamp(Target.X, minX, maxX),
            Target.Y,
            System.Math.Clamp(Target.Z, minZ, maxZ));
    }
}
