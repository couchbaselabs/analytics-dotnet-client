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
using Couchbase.AnalyticsClient.Options;
using Couchbase.AnalyticsClient.Results;

namespace Couchbase.AnalyticsClient.Async;

/// <summary>
/// Represents a handle to a server-side asynchronous query.
/// Obtained from <see cref="Cluster.StartQueryAsync"/> or <see cref="Cluster.QueryHandleFromSerialized"/>.
/// </summary>
public class QueryHandle
{
    private readonly IAnalyticsService _analyticsService;

    /// <summary>
    /// The query handle string used to poll status and fetch results.
    /// This is the path segment after <c>/api/v1/request/status/</c>.
    /// </summary>
    public string Handle { get; }

    /// <summary>
    /// The request ID assigned by the server when the query was submitted.
    /// </summary>
    public string RequestId { get; }

    internal QueryHandle(string handle, string requestId, IAnalyticsService analyticsService)
    {
        Handle = handle ?? throw new ArgumentNullException(nameof(handle));
        RequestId = requestId ?? throw new ArgumentNullException(nameof(requestId));
        _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
    }

    /// <summary>
    /// Fetches the result handle of the asynchronous query from the server.
    /// </summary>
    /// <param name="options">Options for fetching the result handle.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="QueryResultHandle"/> if results are ready, otherwise null.</returns>
    public Task<QueryResultHandle?> FetchResultHandleAsync(FetchResultHandleOptions? options = null, CancellationToken cancellationToken = default)
    {
        options ??= new FetchResultHandleOptions();
        return _analyticsService.FetchResultHandleAsync(this, options, cancellationToken);
    }

    /// <summary>
    /// Fetches the result handle of the asynchronous query from the server.
    /// </summary>
    public Task<QueryResultHandle?> FetchResultHandleAsync(Func<FetchResultHandleOptions, FetchResultHandleOptions> options, CancellationToken cancellationToken = default)
    {
        var fetchOptions = new FetchResultHandleOptions();
        fetchOptions = options.Invoke(fetchOptions);
        return FetchResultHandleAsync(fetchOptions, cancellationToken);
    }

    /// <summary>
    /// Cancels the query on the server. If the query has already completed, this is a no-op.
    /// </summary>
    /// <param name="options">Options for cancellation.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public Task CancelAsync(CancelOptions? options = null, CancellationToken cancellationToken = default)
    {
        options ??= new CancelOptions();
        return _analyticsService.CancelQueryAsync(RequestId, options, cancellationToken);
    }

    /// <summary>
    /// Cancels the query on the server. If the query has already completed, this is a no-op.
    /// </summary>
    public Task CancelAsync(Func<CancelOptions, CancelOptions> options, CancellationToken cancellationToken = default)
    {
        var cancelOptions = new CancelOptions();
        cancelOptions = options.Invoke(cancelOptions);
        return CancelAsync(cancelOptions, cancellationToken);
    }
}
