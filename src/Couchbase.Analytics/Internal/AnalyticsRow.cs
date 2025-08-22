using Couchbase.Text.Json;

namespace Couchbase.Analytics2.Internal;

public sealed class AnalyticsRow
{
    private readonly IJsonToken _token;

    internal AnalyticsRow(IJsonToken token)
    {
        _token = token ?? throw new ArgumentNullException(nameof(token));
    }

    public T ContentAs<T>()
    {
        if (typeof(T) == typeof(byte[]))
        {
            var bytes = _token.ToUtf8Bytes();
            return (T)(object)bytes;
        }

        return _token.ToObject<T>();
    }
}