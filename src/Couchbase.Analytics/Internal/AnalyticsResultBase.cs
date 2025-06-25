using Couchbase.Text.Json;

namespace Couchbase.Analytics2.Internal;

internal abstract class AnalyticsResultBase<T> : IQueryResult<T>
{
    protected readonly Stream ResponseStream;
    protected readonly IDeserializer Serializer;
    private readonly IDisposable? _ownedForCleanup;
    private bool _disposed;

    /// <summary>
    /// Creates a new AnalyticsResultBase.
    /// </summary>
    /// <param name="responseStream"><see cref="Stream"/> to read.</param>
    /// <param name="serializer">The <see cref="ISerializer"/> to use for converting the response to an object.</param>
    /// <param name="ownedForCleanup">Additional object to dispose when complete.</param>
    protected AnalyticsResultBase(Stream responseStream, IDeserializer serializer, IDisposable? ownedForCleanup = null)
    {
        ResponseStream = responseStream ?? throw new ArgumentNullException(nameof(responseStream));
        Serializer = serializer;
        _ownedForCleanup = ownedForCleanup;
    }
    
    public abstract IAsyncEnumerator<T> GetAsyncEnumerator(
        CancellationToken cancellationToken = new CancellationToken());

    public abstract Task InitializeAsync(CancellationToken cancellationToken = default);

    public IAsyncEnumerable<T> Rows { get; protected set; }
    public QueryMetaData MetaData { get; protected set; }
    
    public IReadOnlyList<Error> Errors { get; protected set; }

    public virtual void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        ResponseStream?.Dispose();
        _ownedForCleanup?.Dispose();
    }
}
