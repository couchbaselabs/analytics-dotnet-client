using System.Net;
using Couchbase.Analytics2.Internal.DnsUtil.Strategies;

namespace Couchbase.Analytics2.Internal.DnsUtil;

internal class DnsEndpointResolver
{
    private readonly IDnsRefreshStrategy _refreshStrategy;
    private IPAddress[]? _cachedAddresses;

    public DnsEndpointResolver(IDnsRefreshStrategy refreshStrategy)
    {
        _refreshStrategy = refreshStrategy;
    }

    public async Task<IPAddress[]> ResolveEndpointsAsync(string hostname, CancellationToken cancellationToken)
    {
        _refreshStrategy.OnRequest();

        if (_cachedAddresses != null && !_refreshStrategy.ShouldRefreshDns()) return _cachedAddresses;
        _cachedAddresses = await System.Net.Dns.GetHostAddressesAsync(hostname, cancellationToken).ConfigureAwait(false);
        _refreshStrategy.OnDnsRefreshed();

        return _cachedAddresses;
    }
}