using Conquerors.Core;

namespace Conquerors.Tests.Core;

public class GridTests
{
    [Fact]
    public void IsInside_RespectsBounds()
    {
        Grid g = new(8, 8, 32);
        Assert.True(g.IsInside(new TileCoord(0, 0)));
        Assert.True(g.IsInside(new TileCoord(7, 7)));
        Assert.False(g.IsInside(new TileCoord(-1, 0)));
        Assert.False(g.IsInside(new TileCoord(8, 0)));
        Assert.False(g.IsInside(new TileCoord(0, 8)));
    }

    [Fact]
    public void CanPlace_FailsOnOccupied()
    {
        Grid g = new(8, 8, 32);
        RectInt fp = new(2, 2, 2, 2);
        Assert.True(g.CanPlace(fp));
        g.Occupy(fp);
        Assert.False(g.CanPlace(fp));
        Assert.False(g.CanPlace(new RectInt(3, 3, 2, 2))); // overlapping
        Assert.True(g.CanPlace(new RectInt(4, 4, 2, 2))); // adjacent
    }

    [Fact]
    public void Occupy_ThenFree_RestoresPlacement()
    {
        Grid g = new(8, 8, 32);
        RectInt fp = new(0, 0, 3, 3);
        g.Occupy(fp);
        Assert.False(g.CanPlace(fp));
        g.Free(fp);
        Assert.True(g.CanPlace(fp));
    }

    [Fact]
    public void Occupy_DoubleOccupy_Throws()
    {
        Grid g = new(8, 8, 32);
        g.Occupy(new RectInt(0, 0, 2, 2));
        Assert.Throws<System.InvalidOperationException>(() => g.Occupy(new RectInt(1, 1, 2, 2)));
    }

    [Fact]
    public void Clear_ResetsAllOccupancy()
    {
        Grid g = new(4, 4, 32);
        g.Occupy(new RectInt(0, 0, 4, 4));
        g.Clear();
        Assert.True(g.CanPlace(new RectInt(0, 0, 4, 4)));
    }

    [Fact]
    public void WorldToTile_AndBack_RoundTrips()
    {
        Grid g = new(8, 8, 32);
        TileCoord t = new(3, 5);
        Microsoft.Xna.Framework.Vector2 world = g.TileToWorld(t);
        TileCoord back = g.WorldToTile(world);
        Assert.Equal(t, back);
    }
}
