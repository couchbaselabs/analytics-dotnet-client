using System.Dynamic;
using System.Text.Json;

namespace Couchbase.Core.Internal;

internal sealed class JsonToken : IJsonToken
{
    private readonly JsonElement _element;
    private readonly JsonStreamReader _streamReader;

    /// <summary>
    /// Creates a new NewtonsoftJsonToken.
    /// </summary>
    /// <param name="element">The <seealso cref="JsonElement"/> to wrap.</param>
    /// <param name="streamReader"><see cref="JsonStreamReader"/> to use for deserialization.</param>
    public JsonToken(JsonElement element, JsonStreamReader streamReader)
    {
        _element = element;
        _streamReader = streamReader ?? throw new ArgumentNullException(nameof(streamReader));
    }

    /// <inheritdoc />
    public IJsonToken? this[string key]
    {
        get
        {
            if (_element.TryGetProperty(key, out var value) && value.ValueKind != JsonValueKind.Null)
            {
                return new JsonToken(value, _streamReader);
            }

            return null;
        }
    }

    /// <inheritdoc />
    public T ToObject<T>() => _streamReader.Deserialize<T>(_element)!;

    /// <inheritdoc />
    public T Value<T>()
    {
        if (_element.TryGetValue<T>(out var value))
        {
            return value!;
        }

        throw new InvalidOperationException($"Unable to convert {_element.ValueKind} to {typeof(T)}.");
    }

    /// <inheritdoc />
    public dynamic ToDynamic() => new ExpandoObject();

    public byte[] ToUtf8Bytes()
    {
        return System.Text.Encoding.UTF8.GetBytes(_element.GetRawText());
    }
}