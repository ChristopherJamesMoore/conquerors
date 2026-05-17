using Microsoft.Xna.Framework;

namespace Conquerors.Core;

/// <summary>
/// Tile grid + occupancy. Owns no rendering and no MG dependencies beyond Vector2.
/// </summary>
public sealed class Grid
{
    public int Width { get; }
    public int Height { get; }
    public int TileSize { get; }

    private readonly bool[,] _occupied;

    public Grid(int width, int height, int tileSize)
    {
        if (width <= 0) throw new System.ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new System.ArgumentOutOfRangeException(nameof(height));
        if (tileSize <= 0) throw new System.ArgumentOutOfRangeException(nameof(tileSize));
        Width = width;
        Height = height;
        TileSize = tileSize;
        _occupied = new bool[width, height];
    }

    public RectInt Bounds => new(0, 0, Width, Height);
    public int PixelWidth => Width * TileSize;
    public int PixelHeight => Height * TileSize;

    public bool IsInside(TileCoord t) =>
        t.X >= 0 && t.X < Width && t.Y >= 0 && t.Y < Height;

    public bool IsInside(RectInt r) =>
        r.X >= 0 && r.Y >= 0 && r.Right <= Width && r.Bottom <= Height;

    public bool IsOccupied(TileCoord t) =>
        IsInside(t) && _occupied[t.X, t.Y];

    public bool CanPlace(RectInt footprint)
    {
        if (!IsInside(footprint))
        {
            return false;
        }
        for (int x = footprint.X; x < footprint.Right; x++)
        {
            for (int y = footprint.Y; y < footprint.Bottom; y++)
            {
                if (_occupied[x, y])
                {
                    return false;
                }
            }
        }
        return true;
    }

    public void Occupy(RectInt footprint)
    {
        if (!IsInside(footprint))
        {
            throw new System.ArgumentOutOfRangeException(nameof(footprint), "footprint outside grid");
        }
        for (int x = footprint.X; x < footprint.Right; x++)
        {
            for (int y = footprint.Y; y < footprint.Bottom; y++)
            {
                if (_occupied[x, y])
                {
                    throw new System.InvalidOperationException($"tile ({x},{y}) already occupied");
                }
                _occupied[x, y] = true;
            }
        }
    }

    public void Free(RectInt footprint)
    {
        if (!IsInside(footprint))
        {
            return;
        }
        for (int x = footprint.X; x < footprint.Right; x++)
        {
            for (int y = footprint.Y; y < footprint.Bottom; y++)
            {
                _occupied[x, y] = false;
            }
        }
    }

    public void Clear() => System.Array.Clear(_occupied, 0, _occupied.Length);

    public Vector2 TileToWorld(TileCoord t) => new(t.X * TileSize, t.Y * TileSize);

    public TileCoord WorldToTile(Vector2 world) =>
        new((int)System.Math.Floor(world.X / TileSize),
            (int)System.Math.Floor(world.Y / TileSize));
}
