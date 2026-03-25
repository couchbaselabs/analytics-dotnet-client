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

using Couchbase.AnalyticsClient.Exceptions;
using Couchbase.AnalyticsClient.Internal;
using Couchbase.AnalyticsClient.Query;
using Couchbase.Core.Json;

namespace Couchbase.AnalyticsClient.Async;

/// <summary>
/// Represents the status of an asynchronous query, obtained by polling via
/// <see cref="QueryHandle.FetchStatusAsync"/>.
/// </summary>
public class QueryStatus
{
    private readonly IAnalyticsService _analyticsService;
    private readonly IDeserializer _deserializer;
    private readonly TimeSpan? _requestTimeout;

    /// <summary>
    /// The raw status string from the server (e.g., "queued", "running", "success", "fatal", "timeout").
    /// </summary>
    public string Status { get; }

    /// <summary>
    /// The handle path for fetching results, available when <see cref="AreResultsReady"/> is true.
    /// This is the full path from the status response (e.g., "/api/v1/request/result/{handle}").
    /// </summary>
    internal string? ResultHandle { get; }

    /// <summary>
    /// Errors returned by the server when the query status is "fatal" or "timeout".
    /// </summary>
    internal IReadOnlyList<QueryError>? Errors { get; }

    /// <summary>
    /// Metrics from the status response.
    /// </summary>
    public QueryMetrics? Metrics { get; }

    /// <summary>
    /// The total number of result rows, available when the query has completed successfully.
    /// </summary>
    public long? ResultCount { get; }

    /// <summary>
    /// Partition information from the status response, if available.
    /// Each element contains a handle path and result count for that partition.
    /// </summary>
    public IReadOnlyList<QueryPartition>? Partitions { get; }

    /// <summary>
    /// Whether the result set is ordered. Available when the query has completed successfully.
    /// </summary>
    public bool? ResultSetOrdered { get; }

    /// <summary>
    /// The timestamp when the query was created on the server.
    /// </summary>
    public DateTimeOffset? CreatedAt { get; }

    internal QueryStatus(
        string status,
        string? resultHandle,
        IReadOnlyList<QueryError>? errors,
        QueryMetrics? metrics,
        long? resultCount,
        IReadOnlyList<QueryPartition>? partitions,
        bool? resultSetOrdered,
        DateTimeOffset? createdAt,
        IAnalyticsService analyticsService,
        IDeserializer deserializer,
        TimeSpan? requestTimeout = null)
    {
        Status = status ?? throw new ArgumentNullException(nameof(status));
        ResultHandle = resultHandle;
        Errors = errors;
        Metrics = metrics;
        ResultCount = resultCount;
        Partitions = partitions;
        ResultSetOrdered = resultSetOrdered;
        CreatedAt = createdAt;
        _analyticsService = analyticsService;
        _deserializer = deserializer;
        _requestTimeout = requestTimeout;
    }

    /// <summary>
    /// Returns true if the query has completed successfully and results are ready to be streamed.
    /// </summary>
    public bool AreResultsReady => string.Equals(Status, "success", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Returns true if the query ended with a terminal error status.
    /// Per spec: "fatal" or "timeout".
    /// Also includes "failed" which is not in the spec but has been observed
    /// from the server (e.g., for cancelled queries).
    /// </summary>
    public bool IsError => string.Equals(Status, "fatal", StringComparison.OrdinalIgnoreCase)
                           || string.Equals(Status, "failed", StringComparison.OrdinalIgnoreCase)
                           || string.Equals(Status, "timeout", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// If results are ready, returns a <see cref="QueryHandleResults"/> that can be used to stream the query rows.
    /// If the query ended with an error, returns the error.
    /// This method does not perform network calls.
    /// </summary>
    /// <returns>
    /// A tuple of (<see cref="QueryHandleResults"/>?, <see cref="AnalyticsException"/>?).
    /// When results are ready, the first element is populated and the second is null.
    /// When the query errored, the first element is null and the second contains the error.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the query is still in progress (not "success", "fatal", or "timeout").
    /// </exception>
    public (QueryHandleResults? Results, AnalyticsException? Error) GetResults()
    {
        if (AreResultsReady)
        {
            if (string.IsNullOrWhiteSpace(ResultHandle))
            {
                throw new InvalidOperationException(
                    "Query status indicates success but no result handle was provided by the server.");
            }

            // Strip the leading path prefix to get just the handle portion
            var handle = ResultHandle.StartsWith("/api/v1/request/result/")
                ? ResultHandle["/api/v1/request/result/".Length..]
                : ResultHandle;

            var handleResults = new QueryHandleResults(handle, _analyticsService, _deserializer, _requestTimeout);
            return (handleResults, null);
        }

        if (IsError)
        {
            var error = BuildErrorFromStatus();
            return (null, error);
        }

        throw new InvalidOperationException(
            $"Cannot get results while query is in status '{Status}'. Poll again using FetchStatusAsync().");
    }

    private AnalyticsException BuildErrorFromStatus()
    {
        if (Errors is { Count: > 0 })
        {
            var firstError = Errors[0];
            return firstError.Code switch
            {
                20000 => new InvalidCredentialException(firstError.Message),
                21002 => new AnalyticsTimeoutException($"{firstError.Message}. Error code: {firstError.Code}"),
                _ => new QueryException(firstError.Message) { Code = firstError.Code, ServerMessage = firstError.Message }
            };
        }

        return string.Equals(Status, "timeout", StringComparison.OrdinalIgnoreCase)
            ? new AnalyticsTimeoutException($"Query timed out on the server (status: {Status}).")
            : new AnalyticsException($"Query failed with status: {Status}");
    }
}
