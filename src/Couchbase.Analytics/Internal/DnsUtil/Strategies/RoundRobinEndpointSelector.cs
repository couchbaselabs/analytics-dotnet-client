using System.Net;

namespace Couchbase.Analytics2.Internal.DnsUtil.Strategies;

internal class RoundRobinEndpointSelector : IEndpointSelectionStrategy
{
    private uint _index;

    public RoundRobinEndpointSelector()
    {
        _index = uint.MaxValue; // Will wrap to 0 on first increment
    }

    public int SelectEndpointIndex(IPAddress[] addresses)
    {
        if (addresses.Length == 0) return 0;

        var nextIndex = Interlocked.Increment(ref _index);
        Interlocked.Exchange(ref _index, (uint)(nextIndex % addresses.Length));
        return (int)(nextIndex % addresses.Length);
    }
}