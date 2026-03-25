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
using Couchbase.AnalyticsClient.Query;
using Couchbase.Core.Json;
using Couchbase.Core.Utils;

namespace Couchbase.AnalyticsClient.Options;

/// <summary>
/// Options for starting an asynchronous server-side query via <see cref="Cluster.StartQueryAsync"/>.
/// </summary>
/// <remarks>
/// Similar to <see cref="QueryOptions"/> with these differences:
/// <list type="bullet">
///   <item><see cref="QueryOptions.Timeout"/> is renamed to <see cref="QueryTimeout"/> to disambiguate from <see cref="RequestTimeout"/>.</item>
///   <item><see cref="RequestTimeout"/> controls the per-HTTP-request timeout for SDK calls (status polls, result fetch, etc.).</item>
///   <item><see cref="ResultTTL"/> overrides the server's default TTL for the result set.</item>
/// </list>
/// </remarks>
public record StartQueryOptions
{
    /// <summary>
    /// The server-side query timeout. If unset, the default <see cref="TimeoutOptions"/>'s QueryTimeout will be used.
    /// This is sent to the server to control how long the query is allowed to run.
    /// </summary>
    public TimeSpan? QueryTimeout { get; init; }

    /// <summary>
    /// The per-HTTP-request timeout for SDK operations related to this async query
    /// (e.g., starting the query, polling status, fetching results, cancelling).
    /// If unset, defaults to <see cref="TimeoutOptions.DispatchTimeout"/>.
    /// </summary>
    public TimeSpan? RequestTimeout { get; init; }

    /// <summary>
    /// Optional result TTL that overrides the cluster's default (1 hour) for the query's result set.
    /// Once the TTL expires, the server discards the result set.
    /// The value should be a duration string (e.g., "30m", "2h").
    /// </summary>
    public string? ResultTTL { get; init; }

    /// <summary>
    /// The ClientContextId to be used for the query request. Used to identify the query in logs and profiles.
    /// If none is provided, a new GUID will be generated.
    /// </summary>
    public string ClientContextId { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Named parameters for the query request.
    /// </summary>
    public Dictionary<string, object> NamedParameters { get; init; } = new();

    /// <summary>
    /// Positional parameters for the query request.
    /// </summary>
    public List<object> PositionalParameters { get; init; } = new();

    /// <summary>
    /// The scan consistency for the query request.
    /// </summary>
    public QueryScanConsistency? ScanConsistency { get; init; }

    /// <summary>
    /// Used to deserialize query rows.
    /// </summary>
    public IDeserializer? Deserializer { get; init; }

    /// <summary>
    /// Whether the query is read-only.
    /// </summary>
    public bool? ReadOnly { get; init; }

    /// <summary>
    /// Maximum number of times to retry a request (when the error is retryable).
    /// Overrides <see cref="ClusterOptions.MaxRetries"/> when provided.
    /// </summary>
    [InterfaceStability(StabilityLevel.Volatile)]
    public uint? MaxRetries { get; init; }

    /// <summary>
    /// Raw parameters passed directly to the analytics service for advanced options.
    /// </summary>
    public Dictionary<string, object> Raw { get; init; } = new();

    /// <summary>
    /// The query context (database and scope) applied to the query. Internal use.
    /// </summary>
    internal QueryContext? QueryContext { get; init; }

    internal string GetFormValuesAsJson(string statement)
    {
        return JsonSerializer.Serialize(GetFormValues(statement));
    }

    internal IDictionary<string, object> GetFormValues(string statement)
    {
        statement = CleanStatement(statement);
        var formValues = new Dictionary<string, object>
        {
            { "statement", statement },
            { "client_context_id", ClientContextId },
            { "mode", "async" }
        };

        if (QueryTimeout.HasValue)
        {
            var formTimeout = QueryTimeout.Value.Add(TimeSpan.FromSeconds(5));
            formValues["timeout"] = $"{(int)formTimeout.TotalMilliseconds}ms";
        }

        if (ScanConsistency.HasValue)
        {
            formValues["scan_consistency"] =
                ScanConsistency == QueryScanConsistency.NotBounded ? "not_bounded" : "request_plus";
        }

        if (ReadOnly.HasValue)
        {
            formValues["readonly"] = ReadOnly;
        }

        if (QueryContext is not null)
        {
            formValues["query_context"] = QueryContext.ToString();
        }

        if (!string.IsNullOrWhiteSpace(ResultTTL))
        {
            formValues["result_ttl"] = ResultTTL;
        }

        foreach (var parameter in NamedParameters)
        {
            formValues.Add(parameter.Key.StartsWith('$') ? parameter.Key : $"${parameter.Key}", parameter.Value);
        }

        if (PositionalParameters.Count != 0)
        {
            formValues.Add("args", PositionalParameters.ToArray());
        }

        foreach (var rawParameter in Raw)
        {
            formValues.Add(rawParameter.Key, rawParameter.Value);
        }

        return formValues;
    }

    private static string CleanStatement(string statement)
    {
        if (string.IsNullOrWhiteSpace(statement))
        {
            throw new ArgumentException("statement cannot be null or empty");
        }

        return statement.Trim();
    }

    // Fluent builder methods

    public StartQueryOptions WithQueryTimeout(TimeSpan? queryTimeout) => this with { QueryTimeout = queryTimeout };

    public StartQueryOptions WithRequestTimeout(TimeSpan? requestTimeout) => this with { RequestTimeout = requestTimeout };

    public StartQueryOptions WithResultTTL(string? resultTTL) => this with { ResultTTL = resultTTL };

    public StartQueryOptions WithClientContextId(string clientContextId) => this with { ClientContextId = clientContextId };

    public StartQueryOptions WithScanConsistency(QueryScanConsistency? scanConsistency) => this with { ScanConsistency = scanConsistency };

    public StartQueryOptions WithReadOnly(bool? readOnly) => this with { ReadOnly = readOnly };

    [InterfaceStability(StabilityLevel.Volatile)]
    public StartQueryOptions WithMaxRetries(uint? maxRetries) => this with { MaxRetries = maxRetries };

    public StartQueryOptions WithNamedParameters(Dictionary<string, object> namedParameters) => this with { NamedParameters = namedParameters };

    public StartQueryOptions WithNamedParameter(string name, object value)
    {
        var copy = new Dictionary<string, object>(NamedParameters);
        copy[name] = value;
        return this with { NamedParameters = copy };
    }

    public StartQueryOptions WithPositionalParameters(IEnumerable<object> positionalParameters) =>
        this with { PositionalParameters = new List<object>(positionalParameters) };

    public StartQueryOptions WithPositionalParameter(object parameter)
    {
        var copy = new List<object>(PositionalParameters) { parameter };
        return this with { PositionalParameters = copy };
    }

    public StartQueryOptions WithRawParameters(Dictionary<string, object> rawParameters) => this with { Raw = rawParameters };

    public StartQueryOptions WithRaw(string name, object value)
    {
        var copy = new Dictionary<string, object>(Raw);
        copy[name] = value;
        return this with { Raw = copy };
    }

    public StartQueryOptions WithDeserializer(IDeserializer deserializer) => this with { Deserializer = deserializer };

    internal StartQueryOptions WithQueryContext(QueryContext queryContext) => this with { QueryContext = queryContext };
}
