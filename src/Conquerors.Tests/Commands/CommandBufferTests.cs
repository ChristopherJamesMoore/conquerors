using Conquerors.Commands;
using Conquerors.Core;

namespace Conquerors.Tests.Commands;

public class CommandBufferTests
{
    [Fact]
    public void Empty_Buffer_HasZeroCount()
    {
        CommandBuffer b = new();
        Assert.Equal(0, b.Count);
        Assert.Empty(b.Pending);
    }

    [Fact]
    public void Enqueue_Preserves_Insertion_Order()
    {
        CommandBuffer b = new();
        PlaceBuildingCommand a = new(PlayerId.Local, "collector", new TileCoord(0, 0));
        PlaceBuildingCommand c = new(PlayerId.Local, "barracks", new TileCoord(5, 5));
        b.Enqueue(a);
        b.Enqueue(c);
        Assert.Equal(2, b.Count);
        Assert.Same(a, b.Pending[0]);
        Assert.Same(c, b.Pending[1]);
    }

    [Fact]
    public void Clear_Empties_Buffer()
    {
        CommandBuffer b = new();
        b.Enqueue(new PlaceBuildingCommand(PlayerId.Local, "collector", new TileCoord(0, 0)));
        b.Clear();
        Assert.Equal(0, b.Count);
        Assert.Empty(b.Pending);
    }
}
