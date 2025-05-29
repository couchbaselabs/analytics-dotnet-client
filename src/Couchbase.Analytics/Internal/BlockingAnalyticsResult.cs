namespace Couchbase.Analytics2.Internal;

internal class BlockingAnalyticsResult<T> : AnalyticsResultBase<T>
{
    public BlockingAnalyticsResult(Stream responseStream, IDisposable? ownedForCleanup = null) : base(responseStream, ownedForCleanup)
    {
    }
}
