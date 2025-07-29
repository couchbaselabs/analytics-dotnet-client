using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Couchbase.Text.Json;

/// <summary>
/// Serializes a <see cref="TimeSpan"/> as the number of whole milliseconds in the format
/// "123ms". Does not support deserialization.
/// </summary>
public sealed class MillisecondsStringJsonConverter : JsonConverter<TimeSpan?>
{
    public override TimeSpan? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var stringValue = reader.GetString();
        if (stringValue != null && stringValue.EndsWith("ms"))
        {
            return TimeSpan.FromMilliseconds(double.Parse(stringValue.Split("ms")[0]));
        }

        if(stringValue != null && stringValue.EndsWith("ns"))
        {
            return TimeSpan.FromMilliseconds(double.Parse(stringValue.Split("ns")[0]));
        }

        throw new JsonException($"cannot parse {stringValue}. Only 0.0ms and 0.0ns formats are supported.");
    }

    public override void Write(Utf8JsonWriter writer, TimeSpan? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

#if NET6_0_OR_GREATER
        Span<char> buffer = stackalloc char[32];

        if (!((uint) value.GetValueOrDefault().TotalMilliseconds).TryFormat(buffer, out var written) || written > 30)
        {
            // Fallback if the buffer is too small
            var str = (uint) value.GetValueOrDefault().TotalMilliseconds + "ms";
            writer.WriteStringValue(str);
            return;
        }

        "ms".AsSpan().CopyTo(buffer.Slice(written));

        writer.WriteStringValue(buffer.Slice(0, written + 2));
#else
            var str = (uint) value.GetValueOrDefault().TotalMilliseconds + "ms";

            writer.WriteStringValue(str);
#endif
    }
}