using Couchbase.AnalyticsClient.Results;
using Couchbase.Grpc.Protocol.Columnar;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using Error = Couchbase.Grpc.Protocol.Columnar.Error;

namespace Couchbase.Analytics.Performer.Internal.Utils;

public class PerformerQuery
{
    public PerformerQuery(Task<IQueryResult> queryTask, ContentAs contentAs, CancellationTokenSource cancellationTokenSource)
    {
        QueryTask = queryTask;
        ContentAs = contentAs;
        CancellationTokenSource = cancellationTokenSource;
    }

    private Task<IQueryResult> QueryTask { get; }
    private IQueryResult? QueryResult { get; set; }
    public CancellationTokenSource? CancellationTokenSource { get; }
    public ContentAs ContentAs { get; set; }

    private IAsyncEnumerator<AnalyticsRow>? _cachedEnumerator;

    public async Task<IQueryResult> GetQueryResult()
    {
        return QueryResult ??= await QueryTask.ConfigureAwait(false);
    }

    public async Task<QueryRowResponse> GetNextRow()
    {
        if (QueryResult is null)
        {
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

        QueryRowResponse response;

        if (await _cachedEnumerator!.MoveNextAsync().ConfigureAwait(false))
        {
            response = new QueryRowResponse()
            {
                Success = new QueryRowResponse.Types.Result()
                {
                    Row = new QueryRowResponse.Types.Row()
                    {
                        RowContent = _cachedEnumerator.Current.ContentAsToAnalyticsRow(ContentAs)
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