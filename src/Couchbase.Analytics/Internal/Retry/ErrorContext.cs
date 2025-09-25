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
using System.Text.Json;
using System.Text.Json.Serialization;
using Couchbase.AnalyticsClient.Query;
using Couchbase.Text.Json.Utils;

namespace Couchbase.AnalyticsClient.Internal.Retry;

internal class ErrorContext
{
    public ErrorContext(string clientContextId, LightweightStopwatch stopwatch, TimeSpan queryTimeout)
    {
        ClientContextId = clientContextId;
        Stopwatch = stopwatch;
        QueryTimeout = queryTimeout;
    }
    private LightweightStopwatch Stopwatch { get; }

    [JsonInclude]
    [JsonPropertyName("http_status")]
    internal HttpStatusCode? StatusCode { get; set; }

    [JsonInclude]
    [JsonPropertyName("client_context_id")]
    internal string ClientContextId { get; }

    [JsonInclude]
    [JsonPropertyName("errors")]
    internal List<QueryError> Errors { get; set; } = new();

    [JsonInclude]
    [JsonPropertyName("retry_attempts")]
    internal int RetryAttempts { get; set;  }

    [JsonInclude]
    [JsonPropertyName("elapsed_time")]
    internal TimeSpan ElapsedTime => Stopwatch.Elapsed;

    [JsonInclude]
    [JsonPropertyName("query_timeout")]
    internal TimeSpan QueryTimeout { get; }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this);
    }
}