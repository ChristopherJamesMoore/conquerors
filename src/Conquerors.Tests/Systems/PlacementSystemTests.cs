using Conquerors.Core;
using Conquerors.Entities;
using Conquerors.Systems;

namespace Conquerors.Tests.Systems;

public class PlacementSystemTests
{
    private static PlacementSystem Sys() => new(new[] { "collector", "barracks" });

    [Fact]
    public void TryPlace_WithoutBuildMode_Fails()
    {
        World w = TestWorlds.Fresh();
        PlacementSystem p = Sys();
        bool ok = p.TryPlace(w, new TileCoord(0, 0), out PlacementResult r);
        Assert.False(ok);
        Assert.Equal(PlacementResult.NotInBuildMode, r);
        Assert.Empty(w.Buildings);
    }

    [Fact]
    public void TryPlace_OffMap_Fails()
    {
        World w = TestWorlds.Fresh();
        PlacementSystem p = Sys();
        p.EnterBuildMode();
        bool ok = p.TryPlace(w, new TileCoord(-1, 0), out PlacementResult r);
        Assert.False(ok);
        Assert.Equal(PlacementResult.OffMap, r);
    }

    [Fact]
    public void TryPlace_OnOccupied_Fails()
    {
        World w = TestWorlds.Fresh();
        w.AddBuilding(new Building(w.NextId(), "hq", new TileCoord(5, 5)));
        PlacementSystem p = Sys();
        p.EnterBuildMode();
        bool ok = p.TryPlace(w, new TileCoord(5, 5), out PlacementResult r);
        Assert.False(ok);
        Assert.Equal(PlacementResult.Occupied, r);
    }

    [Fact]
    public void TryPlace_InsufficientCredits_Fails()
    {
        World w = TestWorlds.Fresh(credits: 50);
        PlacementSystem p = Sys();
        p.EnterBuildMode();
        bool ok = p.TryPlace(w, new TileCoord(0, 0), out PlacementResult r);
        Assert.False(ok);
        Assert.Equal(PlacementResult.InsufficientCredits, r);
        Assert.Equal(50, w.Credits);
    }

    [Fact]
    public void TryPlace_Success_DeductsCost_AddsBuilding_OccupiesGrid()
    {
        World w = TestWorlds.Fresh(credits: 500);
        PlacementSystem p = Sys();
        p.EnterBuildMode();
        bool ok = p.TryPlace(w, new TileCoord(2, 2), out PlacementResult r);
        Assert.True(ok);
        Assert.Equal(PlacementResult.Ok, r);
        Assert.Equal(400, w.Credits);
        Assert.Single(w.Buildings);
        Assert.False(w.Grid.CanPlace(new RectInt(2, 2, 2, 2)));
    }

    [Fact]
    public void SelectByIndex_OutOfRange_NoOp()
    {
        PlacementSystem p = Sys();
        Assert.Equal("collector", p.SelectedDefinitionId);
        Assert.False(p.SelectByIndex(-1));
        Assert.False(p.SelectByIndex(99));
        Assert.Equal("collector", p.SelectedDefinitionId);
        Assert.True(p.SelectByIndex(1));
        Assert.Equal("barracks", p.SelectedDefinitionId);
    }

    [Fact]
    public void Toggle_EnterAndExit()
    {
        PlacementSystem p = Sys();
        Assert.False(p.BuildMode);
        p.ToggleBuildMode();
        Assert.True(p.BuildMode);
        p.ToggleBuildMode();
        Assert.False(p.BuildMode);
    }
}
