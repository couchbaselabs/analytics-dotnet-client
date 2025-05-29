namespace Couchbase.Analytics2;

internal interface IQueryResult<out T> : IDisposable, IAsyncEnumerable<T>
{
    IAsyncEnumerable<T> Rows { get; }

    public QueryMetaData MetaData{ get; }

    public IReadOnlyList<Error> Errors { get; }
}
