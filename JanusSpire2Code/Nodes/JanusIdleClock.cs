namespace JanusSpire2.JanusSpire2Code.Nodes;

/// <summary>Presentation-only time; never consumes combat RNG or changes model state.</summary>
internal sealed class JanusIdleClock
{
    internal const double DelaySeconds = 10d;
    private double _elapsed;

    internal void Reset() => _elapsed = 0d;

    internal bool Advance(double delta, bool eligible)
    {
        if (!eligible)
        {
            Reset();
            return false;
        }
        _elapsed += Math.Max(0d, delta);
        if (_elapsed + 0.000001d < DelaySeconds)
            return false;
        Reset();
        return true;
    }
}
