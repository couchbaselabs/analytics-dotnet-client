using System.Text.Json;
using Couchbase.Analytics2.Internal;

namespace Couchbase.Analytics2;

public record ClusterOptions
{
    public ClusterOptions()
    {
        SecurityOptions = new SecurityOptions(this);
    }
    internal ConnectionString ConnectionString { get; init; }
    
    public SecurityOptions SecurityOptions { get; init; }

    public TimeoutOptions TimeoutOptions { get; init; } = new();

    //internal JsonSerializer Serializer { get; init; } //static
}



