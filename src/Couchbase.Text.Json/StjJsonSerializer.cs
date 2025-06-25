using System.Text.Json;

namespace Couchbase.Text.Json;

/// <inheritdoc />
public class StjJsonSerializer(JsonSerializerOptions jsonSerializerOptions) : ISerializer
{
    public StjJsonSerializer() : this(JsonSerializerOptions.Default)
    {
    }
    
    /// <inheritdoc />
    public ValueTask SerializeAsync<T>(Stream stream, T obj,
        CancellationToken cancellationToken = default)
    {
        return new ValueTask(JsonSerializer.SerializeAsync(stream, obj, jsonSerializerOptions, cancellationToken));
    }
}