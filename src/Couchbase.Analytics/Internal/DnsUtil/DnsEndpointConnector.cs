using Couchbase.Analytics2.Internal.DnsUtil.Strategies;

namespace Couchbase.Analytics2.Internal.DnsUtil;
using System.Net;
using System.Net.Sockets;

internal enum EndpointSelectionMode
{
    RoundRobin,
    Random
}

/// <summary>
/// Adapted from https://github.com/MihaZupan/DnsRoundRobin.
/// </summary>
internal sealed class DnsEndpointConnector : IDisposable
{
    private readonly DnsEndpointResolver _dnsResolver;
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
            EndpointSelectionMode.RoundRobin => new RoundRobinEndpointSelector(),
            _ => throw new ArgumentOutOfRangeException(nameof(endpointSelectionMode))
        };
    }

    public async Task<Socket> ConnectAsync(DnsEndPoint endPoint, CancellationToken cancellationToken)
    {
        // Avoid DNS resolution overhead if we're dealing with a single IP address
        if (IPAddress.TryParse(endPoint.Host, out var address))
        {
            return await _connectionManager.ConnectToEndpointsAsync(
                [address],
                endPoint.Port,
                _selectionStrategy,
                cancellationToken);
        }

        // Resolve hostname to IP addresses and connect
        var addresses = await _dnsResolver.ResolveEndpointsAsync(endPoint.Host, cancellationToken);
        return await _connectionManager.ConnectToEndpointsAsync(addresses, endPoint.Port, _selectionStrategy, cancellationToken);
    }

    public void Dispose()
    {
        // Nothing to dispose
    }
}