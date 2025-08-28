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
using System.Text.Json;
using Couchbase.Text.Json;

namespace Couchbase.Analytics2;

public record QueryOptions
{
    public bool AsStreaming { get; set; } = true;

    /// <summary>
    /// Sets the overall timeout for the query request.
    /// If unset, the default <see cref="TimeoutOptions"/>'s QueryTimeout will be used.
    /// </summary>
    public TimeSpan? Timeout { get; set; }

    public string ClientContextId { get; set; } = Guid.NewGuid().ToString();

    public Dictionary<string, object> NamedParameters { get; set; } = new();

    public List<object> PositionalParameters { get; set; } = new();

    public QueryScanConsistency? ScanConsistency { get; set; }

    public TimeSpan? ScanWait { get; set; }

    public ISerializer Serializer { get; set; }

    public bool? ReadOnly { get; set; }

    public uint? MaxRetries { get; set; }

    public Dictionary<string, object> Raw {get; set;} = new();

    internal QueryContext? QueryContext { get; set; }

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
}