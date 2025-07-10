/* ************************************************************
 *
 *    @author Couchbase <info@couchbase.com>
 *    @copyright 2025 Couchbase, Inc.
 *
 *    Licensed under the Apache License, Version 2.0 (the "License");
 *    you may not use this file except in compliance with the License.
 *    You may obtain a copy of the License at
 *
 *        http://www.apache.org/licenses/LICENSE-2.0
 *
 *    Unless required by applicable law or agreed to in writing, software
 *    distributed under the License is distributed on an "AS IS" BASIS,
 *    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *    See the License for the specific language governing permissions and
 *    limitations under the License.
 *
 * ************************************************************/
using System.Net;
using Couchbase.Analytics2.Internal.DnsUtil.Strategies;

namespace Couchbase.Analytics2.Internal.DnsUtil;

internal class DnsEndpointResolver : IDnsEndpointResolver
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