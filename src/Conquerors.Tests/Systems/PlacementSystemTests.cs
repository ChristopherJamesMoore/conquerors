using Conquerors.Commands;
using Conquerors.Core;
using Conquerors.Entities;
using Conquerors.Systems;

namespace Conquerors.Tests.Systems;

public class PlacementSystemTests
{
    private static PlacementSystem Sys() => new(new[] { "collector", "barracks" });

    private static PlaceBuildingCommand Place(string id, int x, int y)
        => new(PlayerId.Local, id, new TileCoord(x, y));

    [Fact]
    public void Apply_OffMap_Rejected()
    {
        World w = TestWorlds.Fresh();
        PlacementSystem p = Sys();
        PlacementResult r = p.Apply(w, Place("collector", -1, 0));
        Assert.Equal(PlacementResult.OffMap, r);
        Assert.Empty(w.Buildings);
    }

    [Fact]
    public void Apply_OnOccupied_Rejected()
    {
        World w = TestWorlds.Fresh();
        w.AddBuilding(new Building(w.NextId(), "hq", new TileCoord(5, 5)));
        PlacementSystem p = Sys();
        PlacementResult r = p.Apply(w, Place("collector", 5, 5));
        Assert.Equal(PlacementResult.Occupied, r);
    }

    [Fact]
    public void Apply_InsufficientCredits_Rejected()
    {
        World w = TestWorlds.Fresh(credits: 50);
        PlacementSystem p = Sys();
        PlacementResult r = p.Apply(w, Place("collector", 0, 0));
        Assert.Equal(PlacementResult.InsufficientCredits, r);
        Assert.Equal(50, w.Credits);
        Assert.Empty(w.Buildings);
    }

    [Fact]
    public void Apply_Success_DeductsCost_AddsBuilding_OccupiesGrid()
    {
        World w = TestWorlds.Fresh(credits: 500);
        PlacementSystem p = Sys();
        PlacementResult r = p.Apply(w, Place("collector", 2, 2));
        Assert.Equal(PlacementResult.Ok, r);
        Assert.Equal(400, w.Credits);
        Assert.Single(w.Buildings);
        Assert.False(w.Grid.CanPlace(new RectInt(2, 2, 2, 2)));
    }

    [Fact]
    public void Apply_IgnoresBuildModeFlag()
    {
        // Build mode is UI state. A well-formed command applies regardless — the input
        // layer is what gates command emission on build mode.
        World w = TestWorlds.Fresh(credits: 500);
        PlacementSystem p = Sys();
        Assert.False(p.BuildMode);
        PlacementResult r = p.Apply(w, Place("collector", 2, 2));
        Assert.Equal(PlacementResult.Ok, r);
        Assert.Single(w.Buildings);
    }

    [Fact]
    public void Check_TakesDefinitionId_Independent_Of_Selection()
    {
        World w = TestWorlds.Fresh(credits: 500);
        PlacementSystem p = Sys();
        Assert.Equal("collector", p.SelectedDefinitionId);
        // Caller can check a different defn than the current selection (used by HUD/ghost).
        Assert.Equal(PlacementResult.Ok, p.Check(w, "barracks", new TileCoord(0, 0)));
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
