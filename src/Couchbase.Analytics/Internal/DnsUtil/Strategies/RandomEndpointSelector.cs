using System.Net;

namespace Couchbase.Analytics2.Internal.DnsUtil.Strategies;

internal class RandomEndpointSelector : IEndpointSelectionStrategy
{
    public int SelectEndpointIndex(IPAddress[] addresses)
    {
        return addresses.Length == 1 ? 1 : Random.Shared.Next(addresses.Length);
    }
}