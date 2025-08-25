using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;

namespace Couchbase.Text.Json;

/// <summary>
/// Serializes and deserializes <see cref="TimeSpan"/> as the number of whole milliseconds in the format
/// "123ms".
/// </summary>
public sealed class MillisecondsStringJsonConverter : JsonConverter<TimeSpan?>
{
    public override TimeSpan? Read(ref Utf8JsonReader reader,
        Type typeToConvert, JsonSerializerOptions options)
    {
        var stringValue = reader.GetString();
        if (stringValue == null)
        {
            return null;
        }

        if (stringValue.EndsWith("ms"))
        {
            if (double.TryParse(stringValue[..^2], NumberStyles.Float, CultureInfo.InvariantCulture, out var numericValue))
            {
                return TimeSpan.FromMilliseconds(numericValue);
            }
        }
        else if (stringValue.EndsWith("ns"))
        {
            if (long.TryParse(stringValue[..^2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var nanoseconds))
            {
                return TimeSpan.FromTicks(nanoseconds / 100);
            }
        }
        else if (stringValue.EndsWith('s'))
        {
            if (double.TryParse(stringValue[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds))
            {
                return TimeSpan.FromSeconds(seconds);
            }
        }

        throw new JsonException(
                $"cannot parse {stringValue}. Only 0.0ms, 0.0ns, and 0.0s formats are supported.");
    }

    public override void Write(Utf8JsonWriter writer, TimeSpan? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

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
    }
}