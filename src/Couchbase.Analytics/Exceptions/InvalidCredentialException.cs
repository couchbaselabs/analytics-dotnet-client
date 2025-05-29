using System.Runtime.Serialization;

namespace Couchbase.Analytics2.Exceptions;

/// <summary>
/// Thrown if the analytics service returns HTTP status code 401 or analytics error code 20000
/// </summary>
public class InvalidCredentialException : AnalyticsException
{
    public InvalidCredentialException()
    {
    }

    protected InvalidCredentialException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public InvalidCredentialException(string? message) : base(message)
    {
    }

    public InvalidCredentialException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
