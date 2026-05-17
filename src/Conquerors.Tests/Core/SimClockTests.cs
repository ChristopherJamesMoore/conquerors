using Conquerors.Core;

namespace Conquerors.Tests.Core;

public class SimClockTests
{
    [Fact]
    public void Fresh_Clock_Has_Zero_Tick_And_Alpha()
    {
        SimClock c = new();
        Assert.Equal(0, c.Tick);
        Assert.Equal(0.0, c.Accumulator);
        Assert.Equal(0f, c.Alpha);
    }

    [Fact]
    public void Advance_Below_Tick_Boundary_Steps_Zero()
    {
        SimClock c = new();
        int steps = c.Advance(0.025);
        Assert.Equal(0, steps);
        Assert.Equal(0, c.Tick);
        Assert.InRange(c.Alpha, 0.49f, 0.51f);
    }

    [Fact]
    public void Advance_Exactly_One_Tick_Steps_Once()
    {
        SimClock c = new();
        int steps = c.Advance(SimClock.SecondsPerTick);
        Assert.Equal(1, steps);
        Assert.Equal(1, c.Tick);
        Assert.InRange(c.Alpha, 0f, 0.001f);
    }

    [Fact]
    public void Advance_Multiple_Ticks_Steps_Each()
    {
        SimClock c = new();
        int steps = c.Advance(SimClock.SecondsPerTick * 3);
        Assert.Equal(3, steps);
        Assert.Equal(3, c.Tick);
    }

    [Fact]
    public void Accumulator_Carries_Across_Calls()
    {
        SimClock c = new();
        // Two 30ms frames = 60ms total → one 50ms tick, 10ms leftover.
        Assert.Equal(0, c.Advance(0.030));
        Assert.Equal(0, c.Tick);
        int steps = c.Advance(0.030);
        Assert.Equal(1, steps);
        Assert.Equal(1, c.Tick);
        Assert.InRange(c.Accumulator, 0.009, 0.011);
    }

    [Fact]
    public void Advance_Caps_Steps_To_Prevent_Spiral()
    {
        SimClock c = new();
        // 10s of elapsed real time would request 200 ticks; cap at default of 5.
        int steps = c.Advance(10.0);
        Assert.Equal(SimClock.DefaultMaxStepsPerAdvance, steps);
        Assert.Equal(SimClock.DefaultMaxStepsPerAdvance, c.Tick);
        // Residual remains in accumulator for subsequent Advance calls.
        Assert.True(c.Accumulator > SimClock.SecondsPerTick);
    }

    [Fact]
    public void Advance_Negative_Or_Zero_Elapsed_Is_NoOp()
    {
        SimClock c = new();
        Assert.Equal(0, c.Advance(0.0));
        Assert.Equal(0, c.Advance(-0.5));
        Assert.Equal(0, c.Tick);
        Assert.Equal(0.0, c.Accumulator);
    }

    [Fact]
    public void Tick_Rate_Constants_Are_Consistent()
    {
        Assert.Equal(20, SimClock.TicksPerSecond);
        Assert.Equal(0.05, SimClock.SecondsPerTick);
        Assert.Equal(0.05f, SimClock.TickDt);
    }
}
