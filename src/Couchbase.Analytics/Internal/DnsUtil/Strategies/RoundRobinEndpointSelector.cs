using System.Net;

namespace Couchbase.Analytics2.Internal.DnsUtil.Strategies;

internal class RoundRobinEndpointSelector : IEndpointSelectionStrategy
{
    private uint _index;

    /// <summary>
    /// Creates a new instance of <see cref="RoundRobinEndpointSelector"/>.
    /// </summary>
    /// <param name="startAtRandom">Determines whether the initial index should be 0 (false) or random (true)</param>
    public RoundRobinEndpointSelector(bool startAtRandom = false)
    {
        // If startAtRandom is true, we initialize _index to a random value.
        // Otherwise, we start at uint.MaxValue (which will wrap at the next increment).
        _index = startAtRandom ? (uint)Random.Shared.Next(int.MaxValue) : uint.MaxValue;
    }

    public int SelectEndpointIndex(IPAddress[] addresses)
    {
        if (addresses.Length == 0) return 0;

        var nextIndex = Interlocked.Increment(ref _index);
        Interlocked.Exchange(ref _index, (uint)(nextIndex % addresses.Length));
        return (int)(nextIndex % addresses.Length);
    }
}