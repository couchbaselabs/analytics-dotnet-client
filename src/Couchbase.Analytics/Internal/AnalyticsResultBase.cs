using Couchbase.Text.Json;

namespace Couchbase.Analytics2.Internal;

internal abstract class AnalyticsResultBase<T> : IQueryResult<T>
{
    protected readonly IJsonStreamReader _jsonReader;
    protected readonly Stream ResponseStream;
    protected readonly IDisposable? _ownedForCleanup;

    /// <summary>
    /// Creates a new AnalyticsResultBase.
    /// </summary>
    /// <param name="responseStream"><see cref="Stream"/> to read.</param>
    /// <param name="ownedForCleanup">Additional object to dispose when complete.</param>
    protected AnalyticsResultBase(Stream responseStream, IDisposable? ownedForCleanup = null)
    {
        ResponseStream = responseStream ?? throw new ArgumentNullException(nameof(responseStream));
        _ownedForCleanup = ownedForCleanup;
    }
    
    protected AnalyticsResultBase(IJsonStreamReader jsonReader, IDisposable? ownedForCleanup = null)
    {
        _jsonReader = jsonReader ?? throw new ArgumentNullException(nameof(jsonReader));
        _ownedForCleanup = ownedForCleanup;
    }

    public abstract IAsyncEnumerator<T> GetAsyncEnumerator(
        CancellationToken cancellationToken = new CancellationToken());

    public abstract Task InitializeAsync(CancellationToken cancellationToken = default);

    public IAsyncEnumerable<T> Rows { get; protected set; }
    public QueryMetaData MetaData { get; protected set; }
    
    public IReadOnlyList<Error> Errors { get; protected set; }

    public void Dispose()
    {
        ResponseStream?.Dispose();
        _ownedForCleanup?.Dispose();
        _jsonReader?.Dispose();
    }
}
