using System.Collections.Concurrent;
using System.Net;
using Couchbase.Analytics.Performer.Internal.Connections;
using Couchbase.Analytics.Performer.Internal.Modes;
using Couchbase.Analytics.Performer.Internal.Utils;
using Couchbase.AnalyticsClient.Async;
using Couchbase.AnalyticsClient.Options;
using Couchbase.AnalyticsClient.Results;
using Couchbase.Core.Utils;
using Couchbase.Grpc.Protocol.Columnar;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Error = Couchbase.Grpc.Protocol.Columnar.Error;

namespace Couchbase.Analytics.Performer.Internal.Services;

internal class AnalyticsPerformerCrossService : ColumnarCrossService.ColumnarCrossServiceBase
{
    private readonly ConcurrentDictionary<string, ClusterConnection> _clusterConnections;
    private readonly ConcurrentDictionary<string, PerformerQuery> _ongoingQueries = new();

    public AnalyticsPerformerCrossService(
        ConcurrentDictionary<string, ClusterConnection> clusterConnections)
    {
        _clusterConnections = clusterConnections;
    }

    public override Task<ExecuteQueryResponse> ExecuteQuery(ExecuteQueryRequest request, ServerCallContext context)
    {
        Serilog.Log.Information("Executing query");
        var stopWatch = LightweightStopwatch.StartNew();
        var initiated = Timestamp.FromDateTime(DateTime.UtcNow);

        var options = new QueryOptions();
        if (request.Options is not null)
        {
            options = request.Options.ToQueryOptions();
        }
        // We always create a CTS and pass the token to the query calls so the driver can request a cancellation,
        // but we don't use it for timing out the queries (the options' Timeout handles it)
        var cts = new CancellationTokenSource();

        var queryHandle = Guid.NewGuid().ToString();

        Task<IQueryResult> queryTask;

        switch (request.LevelCase)
        {
            case ExecuteQueryRequest.LevelOneofCase.ClusterLevel:
                options = options.WithAsStreaming(Mode.PushBasedStreaming == (Mode)request.ClusterLevel.Shared.ModeIndex);
                var clusterConnection = _clusterConnections[request.ClusterLevel.ClusterId];
                queryTask = clusterConnection.ExecuteClusterQuery(request.Statement, options, cts.Token);
                break;
            case ExecuteQueryRequest.LevelOneofCase.ScopeLevel:
                options = options.WithAsStreaming(Mode.PushBasedStreaming == (Mode)request.ScopeLevel.Shared.ModeIndex);
                var clusterConnection2 = _clusterConnections[request.ScopeLevel.ClusterId];
                queryTask = clusterConnection2.ExecuteScopeQuery(request.ScopeLevel.DatabaseName, request.ScopeLevel.ScopeName, request.Statement, options, cts.Token);
                break;
            case ExecuteQueryRequest.LevelOneofCase.None:
            default:
                throw new ArgumentException("No level specified for query");
        }

        var performerQuery = new PerformerQuery(queryTask, request.ContentAs, cts);

        _ongoingQueries.TryAdd(queryHandle, performerQuery);

        var response = new ExecuteQueryResponse()
        {
            QueryHandle = queryHandle,
            Metadata = new ResponseMetadata
            {
                ElapsedNanos = (long)stopWatch.Elapsed.TotalNanoseconds,
                Initiated = initiated
            }
        };

        return Task.FromResult(response);
    }

    public override async Task<EmptyResultOrFailureResponse> QueryResult(QueryResultRequest request, ServerCallContext context)
    {
        Serilog.Log.Information("Executing queryResult");

        EmptyResultOrFailureResponse response;

        var stopwatch = LightweightStopwatch.StartNew();
        var initiated = Timestamp.FromDateTime(DateTime.UtcNow);

        try
        {
            if (!_ongoingQueries.TryGetValue(request.QueryHandle, out var performerQuery))
            {
                throw new KeyNotFoundException("Query handle not present in ongoing queries: " + request.QueryHandle);
            }

            var result = await performerQuery.GetQueryResult().ConfigureAwait(false);

            response = new EmptyResultOrFailureResponse()
            {
                EmptySuccess = result.StatusCode == HttpStatusCode.OK,
                Metadata = new ResponseMetadata
                {
                    ElapsedNanos = (long)stopwatch.Elapsed.TotalNanoseconds,
                    Initiated = initiated
                }
            };
        }
        catch (Exception ex)
        {
            response = new EmptyResultOrFailureResponse()
            {
                EmptySuccess = false,
                Error = ex.ToProtoError()
            };
        }

        return response;
    }

    public override async Task<QueryRowResponse> QueryRow(QueryRowRequest request, ServerCallContext context)
    {
        Serilog.Log.Information("Executing queryRow");

        var response = new QueryRowResponse();

        var stopwatch = LightweightStopwatch.StartNew();
        var initiated = Timestamp.FromDateTime(DateTime.UtcNow);

        if (!_ongoingQueries.TryGetValue(request.QueryHandle, out var performerQuery))
        {
            throw new KeyNotFoundException("Query handle not present in ongoing queries: " + request.QueryHandle);
        }

        // Need to handle 2 types of exception:
        // 1: Awaiting the query
        // 2: Iterating the rows
        try
        {
            await performerQuery.GetQueryResult().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            response.ExecuteQueryFailure = ex.ToProtoError();
            return response;
        }

        try
        {
            response = await performerQuery.GetNextRow(request.ContentAs).ConfigureAwait(false);
            response.Metadata = new ResponseMetadata
            {
                ElapsedNanos = (long)stopwatch.Elapsed.TotalNanoseconds,
                Initiated = initiated
            };
        }
        catch (Exception ex)
        {
            response.RowLevelFailure = ex.ToProtoError();
        }

        return response;
    }

    public override Task<EmptyResultOrFailureResponse> QueryCancel(QueryCancelRequest request, ServerCallContext context)
    {
        Serilog.Log.Information("Executing queryCancel");

        var stopwatch = LightweightStopwatch.StartNew();
        var initiated = Timestamp.FromDateTime(DateTime.UtcNow);

        if (!_ongoingQueries.TryRemove(request.QueryHandle, out var performerQuery))
        {
            throw new KeyNotFoundException("Query handle not present in ongoing queries: " + request.QueryHandle);
        }

        var response = new EmptyResultOrFailureResponse();

        try
        {
            performerQuery.CancellationTokenSource?.Cancel();
            response.EmptySuccess = true;
        }
        catch (Exception)
        {
            //TODO: Map exceptions?
            response.Error = new Error();
            response.Error.Platform = new PlatformError();
            response.Error.Platform.Type = PlatformErrorType.PlatformErrorOther;
        }

        response.Metadata = new ResponseMetadata
        {
            ElapsedNanos = (long)stopwatch.Elapsed.TotalNanoseconds,
            Initiated = initiated
        };

        return Task.FromResult(response);
    }

    public override async Task<QueryResultMetadataResponse> QueryMetadata(QueryMetadataRequest request, ServerCallContext context)
    {
        Serilog.Log.Information("Executing queryMetadata");
        var stopwatch = LightweightStopwatch.StartNew();
        var initiated = Timestamp.FromDateTime(DateTime.UtcNow);

        var response = new QueryResultMetadataResponse();

        try
        {
            if (!_ongoingQueries.TryGetValue(request.QueryHandle, out var performerQuery))
            {
                throw new KeyNotFoundException("Query handle not present in ongoing queries: " + request.QueryHandle);
            }

            var result = await performerQuery.GetQueryResult().ConfigureAwait(false);

            response.Success = new QueryResultMetadataResponse.Types.QueryMetadata();
            response.Success = result.MetaData.ToResponseMetaData();
            response.Metadata = new ResponseMetadata
            {
                ElapsedNanos = (long)stopwatch.Elapsed.TotalNanoseconds,
                Initiated = initiated
            };
        }
        catch (Exception ex)
        {
            response = new QueryResultMetadataResponse();
            response.Failure = ex.ToProtoError();
        }

        return response;
    }

    public override Task<EmptyResultOrFailureResponse> CloseQueryResult(CloseQueryResultRequest request,
        ServerCallContext context)
    {
        Serilog.Log.Information("Executing closeQueryResult");

        var stopwatch = LightweightStopwatch.StartNew();
        var initiated = Timestamp.FromDateTime(DateTime.UtcNow);

        Exception? exception = null;

        if (request.QueryHandle is not null)
        {
            if (!_ongoingQueries.TryRemove(request.QueryHandle, out var performerQuery))
            {
                exception = new KeyNotFoundException("Query handle not present in ongoing queries: " + request.QueryHandle);
            }
        }

        var response = new EmptyResultOrFailureResponse().GetResponseMetaData(stopwatch, initiated, exception);

        return Task.FromResult(response);
    }

    public override async Task<StartQueryResponse> StartQuery(StartQueryRequest request, ServerCallContext context)
    {
        Serilog.Log.Information("Executing startQuery");

        var response = new StartQueryResponse();

        try
        {
            var options = request.Options.ToStartQueryOptions();
            var queryHandle = Guid.NewGuid().ToString();

            QueryHandle sdkHandle;
            switch (request.LevelCase)
            {
                case StartQueryRequest.LevelOneofCase.ClusterLevel:
                    var clusterConn = _clusterConnections[request.ClusterLevel.ClusterId];
                    sdkHandle = await clusterConn.StartClusterQuery(request.Statement, options).ConfigureAwait(false);
                    break;
                case StartQueryRequest.LevelOneofCase.ScopeLevel:
                    var scopeConn = _clusterConnections[request.ScopeLevel.ClusterId];
                    sdkHandle = await scopeConn.StartScopeQuery(
                        request.ScopeLevel.DatabaseName,
                        request.ScopeLevel.ScopeName,
                        request.Statement,
                        options).ConfigureAwait(false);
                    break;
                case StartQueryRequest.LevelOneofCase.None:
                default:
                    throw new ArgumentException("No level specified for query");
            }

            var performerQuery = new PerformerQuery
            {
                AsyncHandle = sdkHandle,
                CancellationTokenSource = new CancellationTokenSource(),
            };
            _ongoingQueries[queryHandle] = performerQuery;

            response.QueryHandle = queryHandle;
        }
        catch (Exception ex)
        {
            response.Failure = ex.ToProtoError();
        }

        return response;
    }

    public override async Task<AsyncFetchStatusResponse> AsyncFetchStatus(AsyncFetchStatusRequest request,
        ServerCallContext context)
    {
        Serilog.Log.Information("Executing asyncFetchStatus");

        var response = new AsyncFetchStatusResponse();

        try
        {
            if (!_ongoingQueries.TryGetValue(request.QueryHandle, out var performerQuery)
                || performerQuery.AsyncHandle is null)
            {
                throw new KeyNotFoundException(
                    "Query handle not present in ongoing async queries: " + request.QueryHandle);
            }

            var status = await performerQuery.AsyncHandle.FetchStatusAsync().ConfigureAwait(false);
            performerQuery.AsyncStatus = status;

            response.QueryStatus = new AsyncFetchStatusResponse.Types.QueryStatusResult
            {
                ResultsReady = status.ResultsReady,
                ToString_ = status.ToString() ?? string.Empty,
            };
        }
        catch (Exception ex)
        {
            response.Failure = ex.ToProtoError();
        }

        return response;
    }

    public override Task<EmptyResultOrFailureResponse> AsyncQueryStatusResultHandle(
        AsyncQueryStatusResultHandleRequest request, ServerCallContext context)
    {
        Serilog.Log.Information("Executing asyncQueryStatusResultHandle");
        var stopwatch = LightweightStopwatch.StartNew();
        var initiated = Timestamp.FromDateTime(DateTime.UtcNow);

        Exception? exception = null;
        try
        {
            if (!_ongoingQueries.TryGetValue(request.QueryHandle, out var performerQuery)
                || performerQuery.AsyncStatus is null)
            {
                throw new KeyNotFoundException(
                    "QueryStatus not present for query: " + request.QueryHandle);
            }

            performerQuery.AsyncResultHandle = performerQuery.AsyncStatus.ResultHandle();
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        var response = new EmptyResultOrFailureResponse()
            .GetResponseMetaData(stopwatch, initiated, exception);
        return Task.FromResult(response);
    }

    public override async Task<EmptyResultOrFailureResponse> AsyncCancelHandle(AsyncCancelHandleRequest request,
        ServerCallContext context)
    {
        Serilog.Log.Information("Executing asyncCancelHandle");
        var stopwatch = LightweightStopwatch.StartNew();
        var initiated = Timestamp.FromDateTime(DateTime.UtcNow);

        Exception? exception = null;
        try
        {
            if (!_ongoingQueries.TryGetValue(request.QueryHandle, out var performerQuery)
                || performerQuery.AsyncHandle is null)
            {
                throw new KeyNotFoundException(
                    "Query handle not present in ongoing async queries: " + request.QueryHandle);
            }

            await performerQuery.AsyncHandle.CancelAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        return new EmptyResultOrFailureResponse().GetResponseMetaData(stopwatch, initiated, exception);
    }

    public override async Task<EmptyResultOrFailureResponse> AsyncFetchResults(AsyncFetchResultsRequest request,
        ServerCallContext context)
    {
        Serilog.Log.Information("Executing asyncFetchResults");
        var stopwatch = LightweightStopwatch.StartNew();
        var initiated = Timestamp.FromDateTime(DateTime.UtcNow);

        Exception? exception = null;
        try
        {
            if (!_ongoingQueries.TryGetValue(request.QueryHandle, out var performerQuery)
                || performerQuery.AsyncResultHandle is null)
            {
                throw new KeyNotFoundException(
                    "Result handle not present for query: " + request.QueryHandle);
            }

            var fetchOptions = new FetchResultsOptions();
            if (request.Options?.Deserializer is not null)
            {
                fetchOptions = fetchOptions.WithDeserializer(request.Options.Deserializer.ToCore());
            }

            var queryResult = await performerQuery.AsyncResultHandle
                .FetchResultsAsync(fetchOptions).ConfigureAwait(false);

            performerQuery.QueryTask = Task.FromResult(queryResult);
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        return new EmptyResultOrFailureResponse().GetResponseMetaData(stopwatch, initiated, exception);
    }

    public override async Task<EmptyResultOrFailureResponse> AsyncDiscardResults(AsyncDiscardResultsRequest request,
        ServerCallContext context)
    {
        Serilog.Log.Information("Executing asyncDiscardResults");
        var stopwatch = LightweightStopwatch.StartNew();
        var initiated = Timestamp.FromDateTime(DateTime.UtcNow);

        Exception? exception = null;
        try
        {
            if (!_ongoingQueries.TryGetValue(request.QueryHandle, out var performerQuery)
                || performerQuery.AsyncResultHandle is null)
            {
                throw new KeyNotFoundException(
                    "Result handle not present for query: " + request.QueryHandle);
            }

            await performerQuery.AsyncResultHandle.DiscardResultsAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        return new EmptyResultOrFailureResponse().GetResponseMetaData(stopwatch, initiated, exception);
    }

    public override Task<EmptyResultOrFailureResponse> CloseAllQueryResults(CloseAllQueryResultsRequest request,
        ServerCallContext context)
    {
        Serilog.Log.Information("Executing closeAllQueryResults");

        var stopwatch = LightweightStopwatch.StartNew();
        var initiated = Timestamp.FromDateTime(DateTime.UtcNow);

        _ongoingQueries.Clear();

        var response = new EmptyResultOrFailureResponse().GetResponseMetaData(stopwatch, initiated);

        return Task.FromResult(response);
    }
}