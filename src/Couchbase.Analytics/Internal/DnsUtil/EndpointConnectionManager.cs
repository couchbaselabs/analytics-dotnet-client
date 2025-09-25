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
using Couchbase.AnalyticsClient.Internal.DnsUtil.Strategies;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Couchbase.AnalyticsClient.Internal.DnsUtil;

internal class EndpointConnectionManager
{
    private readonly TimeSpan _connectionTimeout;
    private readonly ILogger<EndpointConnectionManager> _logger;

    public EndpointConnectionManager(TimeSpan connectionTimeout, ILogger<EndpointConnectionManager>? logger = null)
    {
        _connectionTimeout = connectionTimeout;
        _logger = logger ?? new NullLogger<EndpointConnectionManager>();
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

        var allExceptions = new List<Exception>(addresses.Length);

        for (var i = 0; i <= addresses.Length; i++)
        {
            var currentIndex = selectionStrategy.SelectEndpointIndex(addresses);
            var address = addresses[currentIndex];

            try
            {
                return await ConnectToSingleEndpointAsync(address, port, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Could not connect to endpoint {Address}:{Port} - {Message}", address, port, ex.Message);;
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

            await socket.ConnectAsync(address, port, effectiveToken).ConfigureAwait(false);
            return socket;
        }
        catch (Exception ex)
        {
            socket?.Dispose();

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