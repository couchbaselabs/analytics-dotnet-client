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
using Couchbase.AnalyticsClient.Public.Query;
using Couchbase.Text.Json;
using Couchbase.Text.Json.Utils;

namespace Couchbase.AnalyticsClient.Public.Options;

public record QueryOptions
{
    /// <summary>
    /// If true, the <see cref="IQueryResult"/> will be returned as a streaming result.
    /// </summary>
    public bool AsStreaming { get; init; } = true;

    /// <summary>
    /// Sets the overall timeout for the query request.
    /// If unset, the default <see cref="TimeoutOptions"/>'s QueryTimeout will be used.
    /// Note that if a <see cref="CancellationToken"/> is used on the query call, it may trigger before this timeout.
    /// </summary>
    public TimeSpan? Timeout { get; init; }

    /// <summary>
    /// The ClientContextId to be used for the query request. Used to identify the query in logs and profiles.
    /// If none is provided, a new GUID will be generated.
    /// </summary>
    public string ClientContextId { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Named parameters for the query request.
    /// Use <see cref="WithNamedParameters(Dictionary{string, object})"/> or <see cref="WithNamedParameter(string, object)"/> to create updated copies.
    /// </summary>
    public Dictionary<string, object> NamedParameters { get; init; } = new ();

    /// <summary>
    /// Positional parameters for the query request.
    /// Use <see cref="WithPositionalParameters(IEnumerable{object})"/> or <see cref="WithPositionalParameter(object)"/> to create updated copies.
    /// </summary>
    public List<object> PositionalParameters { get; init; } = new ();

    /// <summary>
    /// The scan consistency for the query request.
    /// </summary>
    public QueryScanConsistency? ScanConsistency { get; init; }

    /// <summary>
    /// Used to deserialize query rows.
    /// Default to <see cref="StjJsonDeserializer"/>
    /// </summary>
    public IDeserializer Deserializer { get; init; } = new StjJsonDeserializer();

    /// <summary>
    /// Whether the query is read-only.
    /// </summary>
    public bool? ReadOnly { get; init; }

    /// <summary>
    /// Maximum number of times to retry a query (when the error is retryable).
    /// Overrides <see cref="ClusterOptions.MaxRetries"/> when provided.
    /// </summary>
    [InterfaceStability(StabilityLevel.Volatile)]
    public uint? MaxRetries { get; init; }

    /// <summary>
    /// Raw parameters passed directly to the analytics service for advanced options.
    /// Use <see cref="WithRawParameters(Dictionary{string, object})"/> or <see cref="WithRaw(string, object)"/> to create updated copies.
    /// </summary>
    public Dictionary<string, object> Raw { get; init; } = new ();

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
            { "client_context_id", ClientContextId }
        };

        if (Timeout.HasValue)
        {
            var formTimeout = Timeout.Value.Add(TimeSpan.FromSeconds(5));
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

        statement = statement.Trim();

        return statement;
    }

    /// <summary>
    /// Sets if the QueryResult should be returned as a streaming result.
    /// </summary>
    /// <param name="asStreaming"></param>
    /// <returns>A copy of the <see cref="QueryOptions"/></returns>
    public QueryOptions WithAsStreaming(bool asStreaming) => this with { AsStreaming = asStreaming };

    /// <summary>
    /// Sets the overall timeout for the query request.
    /// Note that if a CancellationToken is used on the query call, it may trigger before this timeout.
    /// </summary>
    /// <param name="timeout">A TimeSpan representing the timeout</param>
    /// <returns>A copy of the <see cref="QueryOptions"/></returns>
    public QueryOptions WithTimeout(TimeSpan? timeout) => this with { Timeout = timeout };

    /// <summary>
    /// Sets the ClientContextId to be used for the query request.
    /// This is used to identify the query in logs and profiles.
    /// If none is provided, a new GUID will be generated.
    /// </summary>
    /// <param name="clientContextId">A string representing the identifier</param>
    /// <returns>A copy of the <see cref="QueryOptions"/></returns>
    public QueryOptions WithClientContextId(string clientContextId) => this with { ClientContextId = clientContextId };

    /// <summary>
    /// Sets the scan consistency for the query request.
    /// </summary>
    /// <param name="scanConsistency">The <see cref="QueryScanConsistency"/></param>
    /// <returns>A copy of the <see cref="QueryOptions"/></returns>
    public QueryOptions WithScanConsistency(QueryScanConsistency? scanConsistency) => this with { ScanConsistency = scanConsistency };

    /// <summary>
    /// Sets whether the query is read-only.
    /// </summary>
    /// <param name="readOnly">True or false</param>
    /// <returns>A copy of the <see cref="QueryOptions"/></returns>
    public QueryOptions WithReadOnly(bool? readOnly) => this with { ReadOnly = readOnly };

    /// <summary>
    /// Sets the maximum number of times to retry a query (when the error is retryable).
    /// This overrides the <see cref="ClusterOptions.MaxRetries"/> setting.
    /// </summary>
    /// <param name="maxRetries">A positive integer</param>
    /// <returns>A copy of the <see cref="QueryOptions"/></returns>
    public QueryOptions WithMaxRetries(uint? maxRetries) => this with { MaxRetries = maxRetries };


    /// <summary>
    /// Replaces all existing named parameters with the new set.
    /// </summary>
    /// <param name="namedParameters">The new set of named parameters</param>
    /// <returns>A copy of the <see cref="QueryOptions"/></returns>
    public QueryOptions WithNamedParameters(Dictionary<string, object> namedParameters)
    {
        return this with { NamedParameters = namedParameters };
    }

    /// <summary>
    /// Adds or updates a named parameter to the existing ones.
    /// </summary>
    /// <param name="name">The key of the parameter, as a string</param>
    /// <param name="value">The value of the paremter, as an object</param>
    /// <returns>A copy of the <see cref="QueryOptions"/></returns>
    public QueryOptions WithNamedParameter(string name, object value)
    {
        var copy = new Dictionary<string, object>(NamedParameters);
        copy[name] = value;
        return this with { NamedParameters = copy };
    }

    /// <summary>
    /// Replaces all existing positional parameters with the new set.
    /// </summary>
    /// <param name="positionalParameters">An IEnumerable of positional parameters</param>
    /// <returns>A copy of the <see cref="QueryOptions"/></returns>
    public QueryOptions WithPositionalParameters(IEnumerable<object> positionalParameters)
    {
        var copy = new List<object>(positionalParameters);
        return this with { PositionalParameters = new List<object>(copy) };
    }

    /// <summary>
    /// Adds a new positional parameter to the existing ones.
    /// </summary>
    /// <param name="parameter">The positional parameter</param>
    /// <returns>A copy of the <see cref="QueryOptions"/></returns>
    public QueryOptions WithPositionalParameter(object parameter)
    {
        var copy = new List<object>(PositionalParameters);
        copy.Add(parameter);
        return this with { PositionalParameters = copy };
    }

    /// <summary>
    /// Replaces all existing raw parameters with the new set.
    /// </summary>
    /// <param name="rawParameters">A Dictionary of key : string, values : object</param>
    /// <returns>A copy of the <see cref="QueryOptions"/></returns>
    public QueryOptions WithRawParameters(Dictionary<string, object> rawParameters)
    {
        return this with { Raw = rawParameters };
    }

    /// <summary>
    /// Adds or updates a raw parameter to the existing ones.
    /// </summary>
    /// <param name="name">The key of the raw parameter as a string</param>
    /// <param name="value">The value of the parameter as an object</param>
    /// <returns>A copy of the <see cref="QueryOptions"/></returns>
    public QueryOptions WithRaw(string name, object value)
    {
        var copy = new Dictionary<string, object>(Raw);
        copy[name] = value;
        return this with { Raw = copy };
    }

    /// <summary>
    /// Used to deserialize query rows.
    /// Defaults to <see cref="StjJsonDeserializer"/>
    /// </summary>
    /// <param name="deserializer">A deserializer inheriting from <see cref="IDeserializer"/></param>
    /// <returns>A copy of the <see cref="QueryOptions"/></returns>
    public QueryOptions WithDeserializer(IDeserializer deserializer) => this with { Deserializer = deserializer };

    /// <summary>
    /// Adds a QueryContext to the query, which sets the default database and scope for the query.
    /// </summary>
    /// <param name="queryContext">The <see cref="QueryContext"/></param>
    /// <returns>A copy of the <see cref="QueryOptions"/></returns>
    internal QueryOptions WithQueryContext(QueryContext queryContext) => this with { QueryContext = queryContext };
}