using System.Net;

namespace Couchbase.Analytics2.Internal.DnsUtil;

public interface IDnsEndpointResolver
{
    Task<IPAddress[]> ResolveEndpointsAsync(string hostname, CancellationToken cancellationToken);
}