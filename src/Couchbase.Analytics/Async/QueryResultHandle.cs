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
using Couchbase.AnalyticsClient.Query;
using Couchbase.AnalyticsClient.Results;

namespace Couchbase.AnalyticsClient.Async;

/// <summary>
/// Provides access to the results of a completed asynchronous query.
/// </summary>
public class QueryResultHandle
{
    private readonly string _handlePath;
    private readonly IAnalyticsService _analyticsService;
    
    internal string? Status { get; }
    
    internal AsyncQueryMetrics? Metrics { get; }
    
    internal int? ResultCount { get; }

    /// <summary>
    /// The request ID assigned by the server when the query was submitted.
    /// </summary>
    public string RequestId { get; }

    internal QueryResultHandle(string handlePath, string requestId, string responseJson, IAnalyticsService analyticsService)
    {
        _handlePath = handlePath ?? throw new ArgumentNullException(nameof(handlePath));
        RequestId = requestId ?? throw new ArgumentNullException(nameof(requestId));
        _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
        ArgumentException.ThrowIfNullOrWhiteSpace(responseJson);

        using var json = JsonDocument.Parse(responseJson);
        var root = json.RootElement;
        
        Status = root.TryGetProperty("status", out var statusProp) ? statusProp.GetString() : null;
        if (root.TryGetProperty("metrics", out var metricsElement))
        {
            Metrics = JsonSerializer.Deserialize<AsyncQueryMetrics>(metricsElement.GetRawText());
        }
        
        if (root.TryGetProperty("resultCount", out var resultCountProp) && resultCountProp.TryGetInt32(out var resultCount))
        {
            ResultCount = resultCount;
        }
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

    /// <inheritdoc />
    public override string ToString()
    {
        var elapsed = Metrics?.ElapsedTime?.TotalMilliseconds;
        var metricsStr = elapsed.HasValue ? $"{elapsed}ms elapsed" : "none";
        var countStr = ResultCount.HasValue ? $", ResultCount={ResultCount}" : "";
        
        return $"QueryResultHandle [RequestId={RequestId}, Status={Status ?? "unknown"}{countStr}, Metrics={{{metricsStr}}}]";
    }
}
