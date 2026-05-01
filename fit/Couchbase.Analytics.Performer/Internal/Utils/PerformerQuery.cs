using Couchbase.AnalyticsClient.Async;
using Couchbase.AnalyticsClient.Results;
using Couchbase.Grpc.Protocol.Columnar;

namespace Couchbase.Analytics.Performer.Internal.Utils;

public class PerformerQuery
{
    public PerformerQuery(Task<IQueryResult> queryTask, ContentAs contentAs, CancellationTokenSource cancellationTokenSource)
    {
        QueryTask = queryTask;
        ContentAs = contentAs;
        CancellationTokenSource = cancellationTokenSource;
    }

    public PerformerQuery()
    {
    }

    public Task<IQueryResult>? QueryTask { get; set; }
    private IQueryResult? QueryResult { get; set; }
    public CancellationTokenSource? CancellationTokenSource { get; set; }
    public ContentAs? ContentAs { get; set; }

    // Server-async fields
    public QueryHandle? AsyncHandle { get; set; }
    public QueryStatus? AsyncStatus { get; set; }
    public QueryResultHandle? AsyncResultHandle { get; set; }

    private IAsyncEnumerator<AnalyticsRow>? _cachedEnumerator;

    public async Task<IQueryResult> GetQueryResult()
    {
        if (QueryTask is null)
        {
            throw new InvalidOperationException("No query task associated with this query handle.");
        }
        return QueryResult ??= await QueryTask.ConfigureAwait(false);
    }

    public async Task<QueryRowResponse> GetNextRow(ContentAs? rowContentAs = null)
    {
        if (QueryResult is null)
        {
            if (QueryTask is null)
            {
                throw new InvalidOperationException("No query task associated with this query handle.");
            }
            // Do not wrap in try/catch since we want to bubble the Exception to the caller
            // so it can convert it appropriately (e.g. "Invalid Credentials" and such)
            QueryResult = await QueryTask.ConfigureAwait(false);
        }

        if (_cachedEnumerator is null)
        {
            try
            {
                // ReSharper disable once MethodSupportsCancellation
                _cachedEnumerator = QueryResult.GetAsyncEnumerator();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to get async enumerator from query task.", ex);
            }
        }

        var effectiveContentAs = rowContentAs ?? ContentAs
            ?? throw new InvalidOperationException("No ContentAs available for row deserialization.");

        QueryRowResponse response;

        if (await _cachedEnumerator!.MoveNextAsync().ConfigureAwait(false))
        {
            response = new QueryRowResponse()
            {
                Success = new QueryRowResponse.Types.Result()
                {
                    Row = new QueryRowResponse.Types.Row()
                    {
                        RowContent = _cachedEnumerator.Current.ContentAsToAnalyticsRow(effectiveContentAs)
                    }
                }
            };
        }
        else
        {
            response = new QueryRowResponse()
            {
                Success = new QueryRowResponse.Types.Result()
                {
                    EndOfStream = true
                }
            };
        }

        return response;
    }
}
