namespace Couchbase.Analytics2;

public sealed class Error
{
    internal Error(int code, string message)
    {
        this.Code = code;
        this.Message = message;
    }

    public int Code { get; }

    public string Message { get; }
}
