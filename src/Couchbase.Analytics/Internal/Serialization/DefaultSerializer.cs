using System.Text.Json;

namespace Couchbase.Analytics2.Internal.Serialization;

public class DefaultSerializer(JsonSerializerOptions jsonSerializerOptions)
    : IJsonSerializer
{
    public DefaultSerializer() : this(JsonSerializerOptions.Default)
    {
    }

    public ValueTask<T?> DeserializeAsync<T>(Stream stream,
        CancellationToken cancellationToken = default)
    {
        return JsonSerializer.DeserializeAsync<T>(stream, jsonSerializerOptions, cancellationToken);
    }

    public ValueTask SerializeAsync<T>(Stream stream, T obj,
        CancellationToken cancellationToken = default)
    {
        return new ValueTask(JsonSerializer.SerializeAsync(stream, obj, jsonSerializerOptions, cancellationToken));
    }
}