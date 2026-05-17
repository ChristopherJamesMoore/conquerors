using Conquerors.UI;

namespace Conquerors.Tests.UI;

public class FpsCounterTests
{
    [Fact]
    public void ReportsAverageOverWindow()
    {
        FpsCounter c = new(windowSeconds: 0.5);
        // 31 ticks of 1/60s clear the 0.5s window threshold (30 alone falls a hair short
        // due to binary representation of 1/60), so the counter reports ~60fps.
        for (int i = 0; i < 31; i++) c.Tick(1.0 / 60.0);
        Assert.InRange(c.Fps, 58, 62);
    }

    [Fact]
    public void StartsAtZeroUntilFirstWindow()
    {
        FpsCounter c = new(windowSeconds: 1.0);
        c.Tick(0.1);
        Assert.Equal(0, c.Fps);
    }
}
