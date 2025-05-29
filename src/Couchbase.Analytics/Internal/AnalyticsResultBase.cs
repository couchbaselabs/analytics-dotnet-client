namespace Couchbase.Analytics2.Internal;

internal abstract class AnalyticsResultBase<T> : IQueryResult<T>
{
    private readonly Stream _responseStream;
    private readonly IDisposable? _ownedForCleanup;

    /// <summary>
    /// Creates a new AnalyticsResultBase.
    /// </summary>
    /// <param name="responseStream"><see cref="Stream"/> to read.</param>
    /// <param name="ownedForCleanup">Additional object to dispose when complete.</param>
    protected AnalyticsResultBase(Stream responseStream, IDisposable? ownedForCleanup = null)
    {
        _responseStream = responseStream ?? throw new ArgumentNullException(nameof(responseStream));
        _ownedForCleanup = ownedForCleanup;
    }

    public IAsyncEnumerator<T> GetAsyncEnumerator(
        CancellationToken cancellationToken = new CancellationToken())
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<T> Rows { get; protected set; }
    public QueryMetaData MetaData { get; protected set; }
    public IReadOnlyList<Error> Errors { get; protected set; }

    public void Dispose()
    {
        _responseStream?.Dispose();
        _ownedForCleanup?.Dispose();
    }
}
