using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Conquerors.Rendering;

/// <summary>
/// Owns a single <see cref="BasicEffect"/> and a shared unit-cube vertex buffer.
/// Renderers ask it to draw cubes, quads or line lists at a given world matrix;
/// the heavy lifting is one VertexPositionColorNormal shader.
/// </summary>
public sealed class PrimitiveRenderer : System.IDisposable
{
    private readonly GraphicsDevice _gd;
    private readonly BasicEffect _effect;
    private readonly VertexBuffer _cubeVb;
    private readonly IndexBuffer _cubeIb;
    private readonly int _cubePrimitiveCount;

    public PrimitiveRenderer(GraphicsDevice gd)
    {
        _gd = gd;
        _effect = new BasicEffect(gd)
        {
            VertexColorEnabled = true,
            LightingEnabled = true,
            PreferPerPixelLighting = false,
        };
        _effect.EnableDefaultLighting();
        _effect.AmbientLightColor = new Vector3(0.45f, 0.45f, 0.50f);

        BuildCube(out _cubeVb, out _cubeIb, out _cubePrimitiveCount);
    }

    public BasicEffect Effect => _effect;

    public void SetCamera(Matrix view, Matrix projection)
    {
        _effect.View = view;
        _effect.Projection = projection;
    }

    /// <summary>Draw a unit cube (1×1×1 centred on origin) transformed by <paramref name="world"/>.</summary>
    public void DrawCube(Matrix world, Color color)
    {
        _effect.World = world;
        _effect.DiffuseColor = color.ToVector3();
        _effect.Alpha = color.A / 255f;

        _gd.SetVertexBuffer(_cubeVb);
        _gd.Indices = _cubeIb;
        foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _gd.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, _cubePrimitiveCount);
        }
    }

    /// <summary>Draw a filled axis-aligned ground quad (Y=0) without lighting.</summary>
    public void DrawGroundQuad(float x0, float z0, float x1, float z1, Color color)
    {
        VertexPositionColor[] v = new[]
        {
            new VertexPositionColor(new Vector3(x0, 0, z0), color),
            new VertexPositionColor(new Vector3(x1, 0, z0), color),
            new VertexPositionColor(new Vector3(x1, 0, z1), color),
            new VertexPositionColor(new Vector3(x0, 0, z1), color),
        };
        short[] idx = { 0, 1, 2, 0, 2, 3 };

        bool prevLighting = _effect.LightingEnabled;
        _effect.LightingEnabled = false;
        _effect.World = Matrix.Identity;
        _effect.DiffuseColor = Vector3.One;
        _effect.Alpha = 1f;
        foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _gd.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, v, 0, 4, idx, 0, 2);
        }
        _effect.LightingEnabled = prevLighting;
    }

    /// <summary>Draw a list of unlit colored lines (LineList).</summary>
    public void DrawLines(VertexPositionColor[] vertices)
    {
        if (vertices.Length < 2) return;
        bool prevLighting = _effect.LightingEnabled;
        _effect.LightingEnabled = false;
        _effect.World = Matrix.Identity;
        _effect.DiffuseColor = Vector3.One;
        _effect.Alpha = 1f;
        foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _gd.DrawUserPrimitives(PrimitiveType.LineList, vertices, 0, vertices.Length / 2);
        }
        _effect.LightingEnabled = prevLighting;
    }

    public void Dispose()
    {
        _cubeVb.Dispose();
        _cubeIb.Dispose();
        _effect.Dispose();
    }

    private void BuildCube(out VertexBuffer vb, out IndexBuffer ib, out int primitiveCount)
    {
        // Unit cube centred on origin. Six faces × 4 verts (per-face normals).
        Vector3[] faceNormals =
        {
            new(0, 0, 1), new(0, 0, -1),
            new(1, 0, 0), new(-1, 0, 0),
            new(0, 1, 0), new(0, -1, 0),
        };
        Vector3[,] faceCorners =
        {
            { new(-0.5f, -0.5f, 0.5f), new(0.5f, -0.5f, 0.5f), new(0.5f, 0.5f, 0.5f), new(-0.5f, 0.5f, 0.5f) },
            { new(0.5f, -0.5f, -0.5f), new(-0.5f, -0.5f, -0.5f), new(-0.5f, 0.5f, -0.5f), new(0.5f, 0.5f, -0.5f) },
            { new(0.5f, -0.5f, 0.5f), new(0.5f, -0.5f, -0.5f), new(0.5f, 0.5f, -0.5f), new(0.5f, 0.5f, 0.5f) },
            { new(-0.5f, -0.5f, -0.5f), new(-0.5f, -0.5f, 0.5f), new(-0.5f, 0.5f, 0.5f), new(-0.5f, 0.5f, -0.5f) },
            { new(-0.5f, 0.5f, 0.5f), new(0.5f, 0.5f, 0.5f), new(0.5f, 0.5f, -0.5f), new(-0.5f, 0.5f, -0.5f) },
            { new(-0.5f, -0.5f, -0.5f), new(0.5f, -0.5f, -0.5f), new(0.5f, -0.5f, 0.5f), new(-0.5f, -0.5f, 0.5f) },
        };

        VertexPositionNormalTexture[] verts = new VertexPositionNormalTexture[24];
        short[] indices = new short[36];
        for (int f = 0; f < 6; f++)
        {
            for (int c = 0; c < 4; c++)
            {
                verts[f * 4 + c] = new VertexPositionNormalTexture(faceCorners[f, c], faceNormals[f], Vector2.Zero);
            }
            short b = (short)(f * 4);
            indices[f * 6 + 0] = b;
            indices[f * 6 + 1] = (short)(b + 1);
            indices[f * 6 + 2] = (short)(b + 2);
            indices[f * 6 + 3] = b;
            indices[f * 6 + 4] = (short)(b + 2);
            indices[f * 6 + 5] = (short)(b + 3);
        }

        vb = new VertexBuffer(_gd, typeof(VertexPositionNormalTexture), verts.Length, BufferUsage.WriteOnly);
        vb.SetData(verts);
        ib = new IndexBuffer(_gd, IndexElementSize.SixteenBits, indices.Length, BufferUsage.WriteOnly);
        ib.SetData(indices);
        primitiveCount = indices.Length / 3;
    }
}
