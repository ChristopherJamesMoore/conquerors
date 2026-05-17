namespace Conquerors.UI;

/// <summary>
/// Wall-clock FPS sampler. Caller ticks once per drawn frame with the real elapsed
/// time. Reports rolling FPS over a window (~0.5s).
/// </summary>
public sealed class FpsCounter
{
    private readonly double _windowSeconds;
    private double _accum;
    private int _frames;
    public int Fps { get; private set; }

    public FpsCounter(double windowSeconds = 0.5)
    {
        _windowSeconds = windowSeconds;
    }

    public void Tick(double dtSeconds)
    {
        _accum += dtSeconds;
        _frames++;
        if (_accum >= _windowSeconds)
        {
            Fps = (int)System.Math.Round(_frames / _accum);
            _accum = 0;
            _frames = 0;
        }
    }
}
