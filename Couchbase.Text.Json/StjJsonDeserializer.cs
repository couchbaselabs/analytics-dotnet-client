using System.Text.Json;

namespace Couchbase.Text.Json;

/// <inheritdoc />>
public class StjJsonDeserializer(JsonSerializerOptions jsonSerializerOptions): IDeserializer
{
    /// <inheritdoc />
    public StjJsonDeserializer() : this(JsonSerializerOptions.Default)
    {
    }

    /// <inheritdoc />
    public ValueTask<T?> DeserializeAsync<T>(Stream stream,
        CancellationToken cancellationToken = default)
    {
        return JsonSerializer.DeserializeAsync<T>(stream, jsonSerializerOptions, cancellationToken);
    }

    /// <inheritdoc />
    public IJsonStreamReader CreateJsonStreamReader(Stream stream,
        CancellationToken cancellationToken = default)
    {
        return new JsonStreamReader(stream, jsonSerializerOptions);
    }
}