namespace Couchbase.Analytics2;

public sealed class QueryMetaData
{
    public string? RequestId { get; internal set; }

    public QueryMetrics Metrics { get;  internal set;}

    public IReadOnlyList<QueryWarning> Warnings { get;  internal set;}
}
