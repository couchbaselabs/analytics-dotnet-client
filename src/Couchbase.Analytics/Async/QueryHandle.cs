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

using System.Text.Json;
using Couchbase.AnalyticsClient.Internal;

namespace Couchbase.AnalyticsClient.Async;

/// <summary>
/// Represents a handle to a server-side asynchronous query.
/// Obtained from <see cref="Cluster.StartQueryAsync"/> or <see cref="Cluster.QueryHandleFromSerialized"/>.
/// </summary>
public class QueryHandle
{
    private readonly IAnalyticsService _analyticsService;
    private readonly TimeSpan? _requestTimeout;

    /// <summary>
    /// The query handle string used to poll status and fetch results.
    /// This is the path segment after <c>/api/v1/request/status/</c>.
    /// </summary>
    public string Handle { get; }

    /// <summary>
    /// The request ID assigned by the server when the query was submitted.
    /// </summary>
    public string RequestId { get; }

    internal QueryHandle(string handle, string requestId, IAnalyticsService analyticsService, TimeSpan? requestTimeout = null)
    {
        Handle = handle ?? throw new ArgumentNullException(nameof(handle));
        RequestId = requestId ?? throw new ArgumentNullException(nameof(requestId));
        _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
        _requestTimeout = requestTimeout;
    }

    /// <summary>
    /// Fetches the current status of the asynchronous query from the server.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="QueryStatus"/> representing the current state of the query.</returns>
    public async Task<QueryStatus> FetchStatusAsync(CancellationToken cancellationToken = default)
    {
        return await _analyticsService.FetchStatusAsync(Handle, _requestTimeout, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Discards the query results on the server. After this call, the results can no longer be fetched.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    public async Task DiscardResultsAsync(CancellationToken cancellationToken = default)
    {
        await _analyticsService.DiscardResultsAsync(Handle, _requestTimeout, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Cancels the query on the server. If the query has already completed, this is a no-op.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    public async Task CancelAsync(CancellationToken cancellationToken = default)
    {
        await _analyticsService.CancelQueryAsync(RequestId, _requestTimeout, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Serializes this <see cref="QueryHandle"/> to a JSON string so it can be persisted and
    /// later reconstructed via <see cref="Cluster.QueryHandleFromSerialized"/>.
    /// This method does not perform any network operations.
    /// </summary>
    /// <returns>A JSON string containing the handle and request ID.</returns>
    public string Serialize()
    {
        var data = new SerializedQueryHandle(Handle, RequestId);
        return JsonSerializer.Serialize(data);
    }

    /// <summary>
    /// Deserializes a <see cref="QueryHandle"/> from a JSON string previously produced by <see cref="Serialize"/>.
    /// This method does not perform any network operations.
    /// </summary>
    internal static QueryHandle Deserialize(string serializedHandle, IAnalyticsService analyticsService, TimeSpan? requestTimeout = null)
    {
        ArgumentNullException.ThrowIfNull(serializedHandle);

        var data = JsonSerializer.Deserialize<SerializedQueryHandle>(serializedHandle)
                   ?? throw new ArgumentException("Invalid serialized handle format.", nameof(serializedHandle));

        if (string.IsNullOrWhiteSpace(data.Handle) || string.IsNullOrWhiteSpace(data.RequestId))
        {
            throw new ArgumentException("Serialized handle is missing required fields.", nameof(serializedHandle));
        }

        return new QueryHandle(data.Handle, data.RequestId, analyticsService, requestTimeout);
    }

    private record SerializedQueryHandle(string Handle, string RequestId);
}
