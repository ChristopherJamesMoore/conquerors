using Conquerors.Core;
using Conquerors.Entities;
using Conquerors.Systems;

namespace Conquerors.Tests.Systems;

public class ResourceSystemTests
{
    [Fact]
    public void NoBuildings_DoesNotChangeCredits()
    {
        World w = TestWorlds.Fresh();
        ResourceSystem rs = new();
        rs.Update(w, dt: 1f);
        Assert.Equal(500, w.Credits);
    }

    [Fact]
    public void HQ_Generates1PerSecond()
    {
        World w = TestWorlds.Fresh();
        w.AddBuilding(new Building(w.NextId(), "hq", new TileCoord(0, 0), PlayerId.Local));
        ResourceSystem rs = new();
        rs.Update(w, dt: 1f);
        Assert.Equal(501, w.Credits);
    }

    [Fact]
    public void HQ_PlusCollector_Generates3PerSecond()
    {
        World w = TestWorlds.Fresh();
        w.AddBuilding(new Building(w.NextId(), "hq", new TileCoord(0, 0), PlayerId.Local));
        w.AddBuilding(new Building(w.NextId(), "collector", new TileCoord(5, 5), PlayerId.Local));
        ResourceSystem rs = new();
        rs.Update(w, dt: 1f);
        Assert.Equal(503, w.Credits);
    }

    [Fact]
    public void FractionalDt_AccumulatesAcrossFrames()
    {
        World w = TestWorlds.Fresh(credits: 0);
        w.AddBuilding(new Building(w.NextId(), "hq", new TileCoord(0, 0), PlayerId.Local));
        ResourceSystem rs = new();
        // 10 ticks of 0.25s @ +1/s = 2.5 credits. Whole part = 2; fractional carry = 0.5.
        for (int i = 0; i < 10; i++) rs.Update(w, dt: 0.25f);
        Assert.Equal(2, w.Credits);
        Assert.InRange(rs.Carry, 0.49, 0.51);
    }
}
