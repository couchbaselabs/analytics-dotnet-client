namespace Couchbase.Analytics2.Internal;

internal class StreamingAnalyticsResult<T> : AnalyticsResultBase<T>
{
    private IEnumerable<T> _rows;
    private bool _enumerated;

    public StreamingAnalyticsResult(Stream responseStream, IDisposable? ownedForCleanup = null) : base(responseStream, ownedForCleanup)
    {
    }

    public override IAsyncEnumerator<T> GetAsyncEnumerator(
        CancellationToken cancellationToken = new CancellationToken())
    {
        throw new NotImplementedException();
    }

    public override Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
