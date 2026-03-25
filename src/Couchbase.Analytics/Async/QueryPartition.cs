using System.Text.Json.Serialization;

namespace Couchbase.AnalyticsClient.Async;

/// <summary>
/// Represents a partition of a completed asynchronous query's result set,
/// as returned in the FetchStatus response.
/// </summary>
public class QueryPartition
{
    /// <summary>
    /// The handle path for fetching this partition's results.
    /// </summary>
    [JsonPropertyName("handle")]
    public string? Handle { get; set; }

    /// <summary>
    /// The number of result rows in this partition.
    /// </summary>
    [JsonPropertyName("resultCount")]
    public long ResultCount { get; set; }
}
