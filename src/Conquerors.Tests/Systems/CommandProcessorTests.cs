using Conquerors.Commands;
using Conquerors.Core;
using Conquerors.Systems;

namespace Conquerors.Tests.Systems;

public class CommandProcessorTests
{
    private static (CommandProcessor proc, CommandBuffer buf, PlacementSystem placement) Build()
    {
        PlacementSystem placement = new(new[] { "collector", "barracks" });
        CommandBuffer buf = new();
        CommandProcessor proc = new(placement);
        return (proc, buf, placement);
    }

    [Fact]
    public void ProcessAll_Applies_Pending_Commands_Then_Clears()
    {
        World w = TestWorlds.Fresh(credits: 500);
        (CommandProcessor proc, CommandBuffer buf, _) = Build();

        buf.Enqueue(new PlaceBuildingCommand(PlayerId.Local, "collector", new TileCoord(0, 0)));
        proc.ProcessAll(w, buf);

        Assert.Single(w.Buildings);
        Assert.Equal(400, w.Credits);
        Assert.Equal(0, buf.Count);
    }

    [Fact]
    public void ProcessAll_Drops_Invalid_Commands_Without_Throwing()
    {
        World w = TestWorlds.Fresh(credits: 50);
        (CommandProcessor proc, CommandBuffer buf, _) = Build();

        buf.Enqueue(new PlaceBuildingCommand(PlayerId.Local, "collector", new TileCoord(0, 0)));
        proc.ProcessAll(w, buf);

        Assert.Empty(w.Buildings);
        Assert.Equal(50, w.Credits);
        Assert.Equal(0, buf.Count);
    }

    [Fact]
    public void ProcessAll_Preserves_Command_Order()
    {
        // Two collectors at adjacent tiles — second must apply after first occupies its footprint.
        World w = TestWorlds.Fresh(credits: 500);
        (CommandProcessor proc, CommandBuffer buf, _) = Build();

        buf.Enqueue(new PlaceBuildingCommand(PlayerId.Local, "collector", new TileCoord(0, 0)));
        buf.Enqueue(new PlaceBuildingCommand(PlayerId.Local, "collector", new TileCoord(2, 0)));
        proc.ProcessAll(w, buf);

        Assert.Equal(2, w.Buildings.Count);
        Assert.Equal(300, w.Credits);
    }

    [Fact]
    public void ProcessAll_With_Empty_Buffer_NoOp()
    {
        World w = TestWorlds.Fresh(credits: 500);
        (CommandProcessor proc, CommandBuffer buf, _) = Build();

        proc.ProcessAll(w, buf);

        Assert.Empty(w.Buildings);
        Assert.Equal(500, w.Credits);
    }
}
