using System.Diagnostics;

namespace Couchbase.Analytics2.Internal.Utils;

internal struct LightweightStopwatch
{
    private long _startTicks;

    /// <summary>
    /// Creates and starts a new <see cref="LightweightStopwatch"/>.
    /// </summary>
    /// <returns>The <see cref="LightweightStopwatch"/>.</returns>
    public static LightweightStopwatch StartNew() =>
        new()
        {
            _startTicks = Stopwatch.GetTimestamp()
        };

    /// <summary>
    /// Elapsed milliseconds since the stopwatch was started.
    /// </summary>
    /// <remarks>
    /// Resolution is 10-16 milliseconds.
    /// </remarks>
    public readonly long ElapsedMilliseconds => (long)Elapsed.TotalMilliseconds;

    /// <summary>
    /// Elapsed time since the stopwatch was started.
    /// </summary>
    /// <remarks>
    /// Resolution is 10-16 milliseconds.
    /// </remarks>
    public readonly TimeSpan Elapsed => Stopwatch.GetElapsedTime(_startTicks);

    /// <summary>
    /// Restart the stopwatch from zero.
    /// </summary>
    public void Restart()
    {
        _startTicks = Stopwatch.GetTimestamp();
    }
}