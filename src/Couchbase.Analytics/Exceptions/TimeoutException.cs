using System.Runtime.Serialization;

namespace Couchbase.Analytics2.Exceptions;

/// <summary>
/// Thrown if the SDK detects a client-side timeout, or the server returns analytics error code 21002 (server-side timeout).
/// <remarks>This is the user’s only indication that a request is potentially retriable, since the SDK automatically retries retriable operations until the timeout duration elapses.</remarks>
/// </summary>
public class TimeoutException : AnalyticsException
{
    public TimeoutException()
    {
    }

    protected TimeoutException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public TimeoutException(string? message) : base(message)
    {
    }

    public TimeoutException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
