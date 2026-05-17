namespace Conquerors.Core;

/// <summary>Integer rectangle in tile space (origin = top-left, width/height in tiles).</summary>
public readonly record struct RectInt(int X, int Y, int Width, int Height)
{
    public int Left => X;
    public int Top => Y;
    public int Right => X + Width;
    public int Bottom => Y + Height;

    public bool Contains(TileCoord t) =>
        t.X >= X && t.X < Right && t.Y >= Y && t.Y < Bottom;

    public bool Intersects(RectInt o) =>
        X < o.Right && Right > o.X && Y < o.Bottom && Bottom > o.Y;
}
