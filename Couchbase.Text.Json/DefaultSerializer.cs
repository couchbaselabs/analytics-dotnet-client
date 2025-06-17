using System.Text.Json;

namespace Couchbase.Text.Json;

/// <inheritdoc />
public class DefaultSerializer(JsonSerializerOptions jsonSerializerOptions)
    : ISerializer
{
    /// <inheritdoc />
    public DefaultSerializer() : this(JsonSerializerOptions.Default)
    {
    }
    
    /// <inheritdoc />
    public ValueTask<T?> DeserializeAsync<T>(Stream stream,
        CancellationToken cancellationToken = default)
    {
        return JsonSerializer.DeserializeAsync<T>(stream, jsonSerializerOptions, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask SerializeAsync<T>(Stream stream, T obj,
        CancellationToken cancellationToken = default)
    {
        return new ValueTask(JsonSerializer.SerializeAsync(stream, obj, jsonSerializerOptions, cancellationToken));
    }

    /// <inheritdoc />
    public IJsonStreamReader CreateJsonStreamReader(Stream stream,
        CancellationToken cancellationToken = default)
    {
        return new JsonStreamReader(stream, jsonSerializerOptions);
    }
}