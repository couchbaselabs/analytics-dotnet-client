using System.Runtime.Serialization;

namespace Couchbase.Analytics2.Exceptions;

/// <summary>
/// Thrown if the analytics service returns a response with an error code other than 20000 or 21002.
/// </summary>
public class QueryException : ApplicationException
{
    public QueryException()
    {
    }

    protected QueryException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public QueryException(string? message) : base(message)
    {
    }

    public QueryException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    public int Code { get; init; }

    public string? ServerMessage { get; init; }
}
