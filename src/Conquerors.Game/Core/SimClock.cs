namespace Conquerors.Core;

/// <summary>
/// Fixed-timestep sim scheduler. Render runs at the display's framerate; the
/// simulation advances in exact <see cref="SecondsPerTick"/>-second steps. The
/// caller accumulates real elapsed time with <see cref="Advance"/> and runs a
/// sim step for each returned tick.
///
/// Decoupling sim from render is what makes lockstep MP and deterministic
/// replays tractable later — every peer steps the same number of ticks with
/// the same dt regardless of local framerate.
/// </summary>
public sealed class SimClock
{
    public const int TicksPerSecond = 20;
    public const double SecondsPerTick = 1.0 / TicksPerSecond;
    public const float TickDt = (float)SecondsPerTick;

    /// <summary>Cap on ticks executed per <see cref="Advance"/> call — prevents
    /// runaway catch-up when the host has been paused (debugger, GPU hang).
    /// Residual time stays in the accumulator and is consumed on later calls.</summary>
    public const int DefaultMaxStepsPerAdvance = 5;

    private double _accumulator;

    /// <summary>Monotonically-increasing count of completed sim ticks.</summary>
    public int Tick { get; private set; }

    /// <summary>Unconsumed real-time seconds carried into the next tick.</summary>
    public double Accumulator => _accumulator;

    /// <summary>Fraction of the way through the current tick, 0..1. Render uses
    /// this to interpolate between previous and current sim state.</summary>
    public float Alpha => (float)(_accumulator / SecondsPerTick);

    /// <summary>
    /// Accumulate <paramref name="elapsedSeconds"/> of real time and return how
    /// many whole sim ticks should run now. Caller must invoke the sim-step
    /// function exactly that many times.
    /// </summary>
    public int Advance(double elapsedSeconds, int maxStepsPerCall = DefaultMaxStepsPerAdvance)
    {
        if (elapsedSeconds > 0)
        {
            _accumulator += elapsedSeconds;
        }
        int steps = 0;
        while (_accumulator >= SecondsPerTick && steps < maxStepsPerCall)
        {
            _accumulator -= SecondsPerTick;
            Tick++;
            steps++;
        }
        return steps;
    }
}
