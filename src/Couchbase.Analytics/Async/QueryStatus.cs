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
using Couchbase.AnalyticsClient.Query;

namespace Couchbase.AnalyticsClient.Async;

/// <summary>
/// Represents the status of a server-side asynchronous query.
/// Obtained from <see cref="QueryHandle.FetchStatusAsync"/>.
/// </summary>
public class QueryStatus
{
    private readonly string? _resultHandlePath;
    private readonly string _requestId;
    private readonly JsonElement _root;
    private readonly IAnalyticsService _analyticsService;

    internal string? Status { get; }

    internal AsyncQueryMetrics? Metrics { get; }

    internal int? ResultCount { get; }

    internal bool? ResultSetOrdered { get; }

    internal string? CreatedAt { get; }

    internal int PartitionCount { get; }

    /// <summary>
    /// Returns <c>true</c> if and only if the server response provides a handle
    /// for the SDK to retrieve the query results. Otherwise returns <c>false</c>.
    /// This property does not perform network calls.
    /// </summary>
    public bool ResultsReady { get; }

    internal QueryStatus(string requestId, JsonElement root, IAnalyticsService analyticsService)
    {
        _requestId = requestId ?? throw new ArgumentNullException(nameof(requestId));
        _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));

        if (root.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            throw new ArgumentException("The JSON response element must not be empty or null.", nameof(root));
        }

        _root = root;

        // ── Core fields ──
        Status = root.TryGetProperty("status", out var statusProp) ? statusProp.GetString() : null;

        if (root.TryGetProperty("metrics", out var metricsElement))
        {
            Metrics = JsonSerializer.Deserialize<AsyncQueryMetrics>(metricsElement.GetRawText());
        }

        // ResultsReady is true when the server provides a result handle path
        _resultHandlePath = root.TryGetProperty("handle", out var handleProp) ? handleProp.GetString() : null;
        ResultsReady = !string.IsNullOrWhiteSpace(_resultHandlePath);

        // ── Optional diagnostic fields (may be absent depending on server version or query state) ──
        if (root.TryGetProperty("resultCount", out var resultCountProp) && resultCountProp.TryGetInt32(out var resultCount))
        {
            ResultCount = resultCount;
        }

        if (root.TryGetProperty("resultSetOrdered", out var orderedProp) && orderedProp.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            ResultSetOrdered = orderedProp.GetBoolean();
        }

        CreatedAt = root.TryGetProperty("createdAt", out var createdProp) ? createdProp.GetString() : null;

        if (root.TryGetProperty("partitions", out var partProp) && partProp.ValueKind == JsonValueKind.Array)
        {
            PartitionCount = partProp.GetArrayLength();
        }
    }

    /// <summary>
    /// Returns a new <see cref="QueryResultHandle"/> instance.
    /// This method does not perform network calls.
    /// </summary>
    /// <returns>A <see cref="QueryResultHandle"/> that can be used to fetch or discard results.</returns>
    /// <exception cref="InvalidOperationException">Thrown if results are not yet ready.</exception>
    public QueryResultHandle ResultHandle()
    {
        if (!ResultsReady)
        {
            throw new InvalidOperationException(
                $"Results are not ready. Current status: {Status ?? "unknown"}. " +
                "Poll again with FetchStatusAsync() until ResultsReady is true.");
        }

        return new QueryResultHandle(_resultHandlePath!, _requestId, _root, _analyticsService);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        var parts = new List<string>
        {
            $"Status={Status ?? "unknown"}",
            $"ResultsReady={ResultsReady}"
        };

        if (ResultCount.HasValue)
        {
            parts.Add($"ResultCount={ResultCount}");
        }

        if (ResultSetOrdered.HasValue)
        {
            parts.Add($"ResultSetOrdered={ResultSetOrdered}");
        }

        if (PartitionCount > 0)
        {
            parts.Add($"Partitions={PartitionCount}");
        }

        if (CreatedAt is not null)
        {
            parts.Add($"CreatedAt={CreatedAt}");
        }

        if (Metrics is not null)
        {
            var metricParts = new List<string>();

            if (Metrics.ElapsedTime.HasValue)
                metricParts.Add($"ElapsedTime={Metrics.ElapsedTime.Value.TotalMilliseconds:F1}ms");
            if (Metrics.ExecutionTime.HasValue)
                metricParts.Add($"ExecutionTime={Metrics.ExecutionTime.Value.TotalMilliseconds:F1}ms");
            if (Metrics.CompileTime.HasValue)
                metricParts.Add($"CompileTime={Metrics.CompileTime.Value.TotalMilliseconds:F1}ms");
            if (Metrics.QueueWaitTime.HasValue)
                metricParts.Add($"QueueWaitTime={Metrics.QueueWaitTime.Value.TotalMilliseconds:F1}ms");
            if (Metrics.ProcessedObjects.HasValue)
                metricParts.Add($"ProcessedObjects={Metrics.ProcessedObjects}");

            parts.Add(metricParts.Count > 0
                ? $"Metrics={{{string.Join(", ", metricParts)}}}"
                : "Metrics={none}");
        }

        return $"QueryStatus [{string.Join(", ", parts)}]";
    }
}
