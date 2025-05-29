namespace Couchbase.Analytics2;

public class QueryResult<T> : IQueryResult<T>
{
    private Stream _responseStream;
    private readonly IDisposable _ownedForCleanup;

    internal QueryResult(Stream responseStream, IDisposable ownedForCleanup)
    {
        _responseStream = responseStream;
        _ownedForCleanup = ownedForCleanup;
    }

    public IAsyncEnumerable<T> Rows { get; set; }

    public QueryMetaData MetaData { get; set; }

    public IReadOnlyList<Error> Errors { get; set; }

    private string Status { get; }

    public IAsyncEnumerator<T> GetAsyncEnumerator(
        CancellationToken cancellationToken = new CancellationToken())
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}






