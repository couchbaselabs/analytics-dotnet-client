using System.Text.Json;

namespace Couchbase.Analytics2;

public record ClusterOptions
{
    public SecurityOptions SecurityOptions { get; init; } = new();

    public TimeoutOptions TimeoutOptions { get; init; } = new();

    //internal JsonSerializer Serializer { get; init; } //static
}



