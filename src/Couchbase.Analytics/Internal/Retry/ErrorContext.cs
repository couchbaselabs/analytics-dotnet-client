using System.Net;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Couchbase.Analytics2;
using Couchbase.Text.Json;
using Couchbase.Text.Json.Utils;

namespace Couchbase.Analytics2.Internal.Retry;

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
    internal List<Error> Errors { get; } = new();

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