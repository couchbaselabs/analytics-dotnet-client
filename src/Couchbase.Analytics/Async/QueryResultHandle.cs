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

using Couchbase.AnalyticsClient.Internal;
using Couchbase.AnalyticsClient.Options;
using Couchbase.AnalyticsClient.Results;

namespace Couchbase.AnalyticsClient.Async;

/// <summary>
/// Provides access to the results of a completed asynchronous query.
/// </summary>
public class QueryResultHandle
{
    private readonly string _handlePath;
    private readonly IAnalyticsService _analyticsService;

    /// <summary>
    /// The request ID assigned by the server when the query was submitted.
    /// </summary>
    public string RequestId { get; }

    internal QueryResultHandle(string handlePath, string requestId, IAnalyticsService analyticsService)
    {
        _handlePath = handlePath ?? throw new ArgumentNullException(nameof(handlePath));
        RequestId = requestId ?? throw new ArgumentNullException(nameof(requestId));
        _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
    }

    /// <summary>
    /// Fetches the results of the query from the server.
    /// </summary>
    /// <param name="options">Options for fetching the results.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>An <see cref="IQueryResult"/> that can be used to enumerate the result rows.</returns>
    public Task<IQueryResult> FetchResultsAsync(FetchResultsOptions? options = null, CancellationToken cancellationToken = default)
    {
        options ??= new FetchResultsOptions();
        return _analyticsService.FetchResultsAsync(RequestId, _handlePath, options, cancellationToken);
    }

    /// <summary>
    /// Fetches the results of the query from the server.
    /// </summary>
    public Task<IQueryResult> FetchResultsAsync(Func<FetchResultsOptions, FetchResultsOptions> options, CancellationToken cancellationToken = default)
    {
        var fetchOptions = new FetchResultsOptions();
        fetchOptions = options.Invoke(fetchOptions);
        return FetchResultsAsync(fetchOptions, cancellationToken);
    }

    /// <summary>
    /// Discards the query results on the server. After this call, the results can no longer be fetched.
    /// </summary>
    /// <param name="options">Options for discarding results.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public Task DiscardResultsAsync(DiscardResultsOptions? options = null, CancellationToken cancellationToken = default)
    {
        options ??= new DiscardResultsOptions();
        return _analyticsService.DiscardResultsAsync(RequestId, _handlePath, options, cancellationToken);
    }

    /// <summary>
    /// Discards the query results on the server. After this call, the results can no longer be fetched.
    /// </summary>
    public Task DiscardResultsAsync(Func<DiscardResultsOptions, DiscardResultsOptions> options, CancellationToken cancellationToken = default)
    {
        var discardOptions = new DiscardResultsOptions();
        discardOptions = options.Invoke(discardOptions);
        return DiscardResultsAsync(discardOptions, cancellationToken);
    }
}
