using System.Net;

namespace Couchbase.Analytics2.Internal.DnsUtil.Strategies;

internal interface IEndpointSelectionStrategy
{
    int SelectEndpointIndex(IPAddress[] addresses);
}