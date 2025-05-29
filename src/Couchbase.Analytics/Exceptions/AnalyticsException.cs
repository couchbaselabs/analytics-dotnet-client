using System.Runtime.Serialization;

namespace Couchbase.Analytics2.Exceptions;

/// <summary>
/// Base exception type for Analytics.
/// </summary>
public class AnalyticsException : Exception
{
    public AnalyticsException()
    {
    }

    protected AnalyticsException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public AnalyticsException(string? message) : base(message)
    {
    }

    public AnalyticsException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
