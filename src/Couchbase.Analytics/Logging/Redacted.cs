using System.Runtime.InteropServices;

namespace Couchbase.AnalyticsClient.Logging;

/// <summary>
/// Wraps a value in an optional pair of redaction tags when written to a string.
/// String formatting is delayed to the call to <see cref="ToString()"/>.
/// This avoids the string formatting cost for disabled log levels.
/// </summary>
/// <remarks>
/// <para>
/// Since this type is a structure, it avoids heap allocations so long as we're using strongly typed
/// logging mechanisms to avoid boxing (i.e. <c>[LoggerMessage]</c> source generators).
/// </para>
/// <para>
/// Because this type implements <see cref="ISpanFormattable"/> it also avoids string allocations when
/// used in string interpolation expressions.
/// </para>
/// </remarks>
[StructLayout(LayoutKind.Auto)]
internal readonly struct Redacted<T> : ISpanFormattable
{
    private readonly T _value;
    private readonly string? _redactionType;

    /// <summary>
    /// Creates a no-op redaction — the value is not marked for redaction.
    /// </summary>
    public Redacted(T value) : this(value, null)
    {
    }

    /// <summary>
    /// Creates a redaction of the given type.
    /// </summary>
    /// <param name="value">Value to wrap.</param>
    /// <param name="redactionType">The type of redaction (e.g. "ud", "md", "sd"), or null to not redact.</param>
    public Redacted(T value, string? redactionType)
    {
        _value = value;
        _redactionType = redactionType;
    }

    public override string ToString() => ToString(null, null);

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        if (_redactionType is null)
        {
            return _value is IFormattable formattable
                ? formattable.ToString(null, formatProvider)
                : _value?.ToString() ?? "";
        }

        Span<char> buffer = stackalloc char[128];
        return string.Create(formatProvider, buffer, $"<{_redactionType}>{_value}</{_redactionType}>");
    }

    /// <inheritdoc />
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        return _redactionType is not null
            ? destination.TryWrite(provider, $"<{_redactionType}>{_value}</{_redactionType}>", out charsWritten)
            : destination.TryWrite(provider, $"{_value}", out charsWritten);
    }
}
