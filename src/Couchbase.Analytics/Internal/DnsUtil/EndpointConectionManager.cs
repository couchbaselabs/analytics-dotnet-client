using System.Net;
using System.Net.Sockets;
using Couchbase.Analytics2.Internal.DnsUtil.Strategies;

namespace Couchbase.Analytics2.Internal.DnsUtil;

internal class EndpointConnectionManager
{
    private readonly TimeSpan _connectionTimeout;

    public EndpointConnectionManager(TimeSpan connectionTimeout)
    {
        _connectionTimeout = connectionTimeout;
    }

    public async Task<Socket> ConnectToEndpointsAsync(
        IPAddress[] addresses,
        int port,
        IEndpointSelectionStrategy selectionStrategy,
        CancellationToken cancellationToken)
    {
        if (addresses.Length == 0)
        {
            throw new InvalidOperationException("No addresses provided for connection.");
        }

        var allExceptions = new List<Exception>();
        var startIndex = selectionStrategy.SelectEndpointIndex(addresses);

        // Try all addresses starting from the selected one
        for (var attempt = 0; attempt < addresses.Length; attempt++)
        {
            var currentIndex = (startIndex + attempt) % addresses.Length;
            var address = addresses[currentIndex];

            try
            {
                return await ConnectToSingleEndpointAsync(address, port, cancellationToken);
            }
            catch (Exception ex)
            {
                allExceptions.Add(ex);
            }
        }

        // If all endpoints failed, throw. This should be caught internally to throw AnalyticsException.
        throw new AggregateException("Failed to connect to all available endpoints.", allExceptions);
    }

    private async Task<Socket> ConnectToSingleEndpointAsync(IPAddress address, int port, CancellationToken cancellationToken)
    {
        Socket? socket = null;
        CancellationTokenSource? timeoutCts = null;

        try
        {
            // Create appropriate socket for the address family
            if (Socket.OSSupportsIPv6 && address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
                if (address.IsIPv4MappedToIPv6)
                {
                    socket.DualMode = true;
                }
            }
            else if (Socket.OSSupportsIPv4 && address.AddressFamily == AddressFamily.InterNetwork)
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            }
            else
            {
                throw new NotSupportedException($"Address family {address.AddressFamily} is not supported.");
            }

            // Disable Nagle's algorithm
            socket.NoDelay = true;

            // Setup timeout if specified
            var effectiveToken = cancellationToken;
            if (_connectionTimeout != Timeout.InfiniteTimeSpan)
            {
                timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(_connectionTimeout);
                effectiveToken = timeoutCts.Token;
            }

            await socket.ConnectAsync(address, port, effectiveToken);
            return socket;
        }
        catch (Exception ex)
        {
            socket?.Dispose();

            // Wrap timeout exceptions with more descriptive message
            if (timeoutCts?.IsCancellationRequested == true)
            {
                throw new TimeoutException(
                    $"Failed to connect to endpoint {address}:{port} within the specified timeout of {_connectionTimeout.TotalSeconds:N2} seconds.",
                    ex);
            }

            throw;
        }
        finally
        {
            timeoutCts?.Dispose();
        }
    }
}