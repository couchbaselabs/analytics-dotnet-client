namespace Couchbase.Analytics2.Internal;

internal class StreamingAnalyticsResult<T> : AnalyticsResultBase<T>
{
    public StreamingAnalyticsResult(Stream responseStream, IDisposable? ownedForCleanup = null) : base(responseStream, ownedForCleanup)
    {
    }
}
