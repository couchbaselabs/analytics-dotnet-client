#region License
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
#endregion

using System.Net;
using System.Net.Sockets;
using Couchbase.AnalyticsClient.DnsUtil;
using Couchbase.AnalyticsClient.Internal.DnsUtil.Strategies;

namespace Couchbase.AnalyticsClient.Internal.DnsUtil;

/// <summary>
/// Adapted from https://github.com/MihaZupan/DnsRoundRobin.
/// </summary>
internal sealed class DnsEndpointConnector : IDisposable
{
    /// <summary>
    /// Test hook: when true, forces DNS resolution even when the host is an IP.
    /// This allows tests to use the <see cref="IDnsEndpointResolver"/> path.
    /// </summary>
    internal static bool ForceDnsResolution { get; set; } = false;

    private readonly IDnsEndpointResolver _dnsResolver;
    private readonly EndpointConnectionManager _connectionManager;
    private readonly IEndpointSelectionStrategy _selectionStrategy;

    /// <summary>
    /// Creates a new <see cref="DnsEndpointConnector"/>, which allows configuring the connection to a hostname's resolved IPs from an Http handler.
    /// </summary>
    /// <param name="refreshStrategy">The DNS refresh strategy to use for determining when to refresh DNS records.</param>
    /// <param name="connectTimeout">Maximum amount of time allowed for a connection attempt to any individual endpoint.</param>
    /// <param name="endpointSelectionMode">Determines how endpoints are selected from the DNS record. Defaults to RoundRobin.</param>
    public DnsEndpointConnector(
        IDnsRefreshStrategy refreshStrategy,
        TimeSpan connectTimeout,
        EndpointSelectionMode endpointSelectionMode = EndpointSelectionMode.RoundRobin)
    {
        _dnsResolver = new DnsEndpointResolver(refreshStrategy ?? throw new ArgumentNullException(nameof(refreshStrategy)));
        _connectionManager = new EndpointConnectionManager(connectTimeout);

        _selectionStrategy = endpointSelectionMode switch
        {
            EndpointSelectionMode.Random => new RandomEndpointSelector(),
            EndpointSelectionMode.RandomFromUnusedEndpoints => new RandomEndpointSelector(pickFromUnused: true),
            EndpointSelectionMode.RoundRobin => new RoundRobinEndpointSelector(),
            EndpointSelectionMode.RoundRobinRandomStart => new RoundRobinEndpointSelector(startAtRandom: true),
            _ => throw new ArgumentOutOfRangeException(nameof(endpointSelectionMode))
        };
    }

    public async Task<Socket> ConnectAsync(DnsEndPoint endPoint, CancellationToken cancellationToken)
    {
        // Avoid DNS resolution overhead if we're dealing with a single IP address
        if (!ForceDnsResolution && IPAddress.TryParse(endPoint.Host, out var address))
        {
            return await _connectionManager.ConnectToEndpointsAsync(
                [address],
                endPoint.Port,
                _selectionStrategy,
                cancellationToken).ConfigureAwait(false);
        }

        // Resolve hostname to IP addresses and connect
        var addresses = await _dnsResolver.ResolveEndpointsAsync(endPoint.Host, cancellationToken).ConfigureAwait(false);
        return await _connectionManager.ConnectToEndpointsAsync(addresses, endPoint.Port, _selectionStrategy, cancellationToken).ConfigureAwait(false);
    }

    public void Dispose()
    {
        // Nothing to dispose
    }
}