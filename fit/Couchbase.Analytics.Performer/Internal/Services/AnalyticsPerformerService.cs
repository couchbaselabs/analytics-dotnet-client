using System.Collections.Concurrent;
using Couchbase.Analytics.Performer.Internal.Connections;
using Couchbase.Analytics.Performer.Internal.Exceptions;
using Couchbase.Analytics.Performer.Internal.Modes;
using Couchbase.Analytics.Performer.Internal.Utils;
using Couchbase.AnalyticsClient;
using Couchbase.Core.Utils;
using Couchbase.Grpc.Protocol.Columnar;
using Couchbase.Grpc.Protocol.Shared;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Exception = System.Exception;

namespace Couchbase.Analytics.Performer.Internal.Services;

internal class AnalyticsPerformerService : ColumnarService.ColumnarServiceBase
{
    public AnalyticsPerformerService(ConcurrentDictionary<string, ClusterConnection> clusters)
    {
        Clusters = clusters;
    }
    private ConcurrentDictionary<string, ClusterConnection> Clusters { get; }

    public override Task<EmptyResultOrFailureResponse> SetCredential(SetCredentialRequest request, ServerCallContext context)
    {
        var response = new EmptyResultOrFailureResponse();
        var stopWatch = LightweightStopwatch.StartNew();
        var initiated = Timestamp.FromDateTime(DateTime.UtcNow);

        try
        {
            var newCredential = request.Credential.ToCore();

            if (Clusters.TryGetValue(request.ExecutionContext.ClusterId, out var clusterConnection))
            {
                clusterConnection.Cluster.UpdateCredential(newCredential);
            }
            response = response.GetResponseMetaData(stopWatch, initiated);
        }
        catch (Exception ex)
        {
            response.GetResponseMetaData(stopWatch, initiated, ex);
        }

        return Task.FromResult(response);
    }

    public override Task<EmptyResultOrFailureResponse> ClusterNewInstance(ClusterNewInstanceRequest request,
        ServerCallContext context)
    {
        var stopWatch = LightweightStopwatch.StartNew();
        var initiated = Timestamp.FromDateTime(DateTime.UtcNow);

        Serilog.Log.Information("Creating new cluster instance: {ConnectionString}", request.ConnectionString);

        var response = new EmptyResultOrFailureResponse();
        try
        {
            foreach (var tunable in request.Tunables)
            {
                Environment.SetEnvironmentVariable(tunable.Key, tunable.Value);
            }

            var sdkCluster = Clusters.GetOrAdd(request.ClusterConnectionId,
                (_) =>
                {
                    Serilog.Log.Information(
                        "No cached cluster found creating new cluster: {ConnectionString}",
                        request.ConnectionString);

                    var cluster = Cluster.Create(request.ConnectionString,
                        request.Credential.ToCore(), request.ToSdkQueryOptions());

                    return new(request, cluster);
                });

            Serilog.Log.Information(
                 "Created or using new cluster instance in {Seconds}: {ConnectionString}",
                 stopWatch.Elapsed, request.ConnectionString);

            response.GetResponseMetaData(stopWatch, initiated);
        }
        catch (Exception ex)
        {
            response.GetResponseMetaData(stopWatch, initiated, ex);
        }

        return Task.FromResult(response);
    }

    public override Task<FetchPerformerCapsResponse> FetchPerformerCaps(FetchPerformerCapsRequest request,
        ServerCallContext context)
    {
        var stopWatch = LightweightStopwatch.StartNew();
        Serilog.Log.Information("Calling FetchPerformerCaps");

        var response = new FetchPerformerCapsResponse
        {
            AnalyticsProduct = AnalyticsProduct.Analytics,
            Sdk = SDK.Net
        };

        try
        {
            response.SupportsCertificateCredential = true;
            response.SupportsJwtCredential = true;
            response.SupportsSetCredential = true;

            response.ClusterNewInstance.Add((int)Mode.PushBasedStreaming, new PerApiElementClusterNewInstance { SupportsDispatchTimeout = true });
            response.ClusterNewInstance.Add((int)Mode.Buffered, new PerApiElementClusterNewInstance { SupportsDispatchTimeout = true });

            response.ClusterClose.Add((int)Mode.PushBasedStreaming, new PerApiElementClusterClose());
            response.ClusterClose.Add((int)Mode.Buffered, new PerApiElementClusterClose());

            var executeQueryBuffered = new PerApiElementExecuteQuery
            {
                ExecuteQueryReturns = PerApiElementExecuteQuery
                    .Types.ExecuteQueryReturns.QueryResult,
                RowIteration = PerApiElementExecuteQuery
                    .Types.RowIteration.Buffered,
                RowDeserialization = PerApiElementExecuteQuery
                    .Types.RowDeserialization.StaticRowTypingIndividual,
                SupportsCustomDeserializer = false
            };

            var executeQueryStreaming = new PerApiElementExecuteQuery
            {
                ExecuteQueryReturns = PerApiElementExecuteQuery.Types
                    .ExecuteQueryReturns.QueryMetadata,
                RowIteration = PerApiElementExecuteQuery.Types.RowIteration
                    .StreamingPushBased,
                RowDeserialization = PerApiElementExecuteQuery.Types
                    .RowDeserialization.StaticRowTypingIndividual,
                SupportsCustomDeserializer = false
            };

            response.ClusterExecuteQuery.Add((int)Mode.Buffered,
                executeQueryBuffered);
            response.ClusterExecuteQuery.Add((int)Mode.PushBasedStreaming,
                executeQueryStreaming);
            response.ScopeExecuteQuery.Add((int)Mode.Buffered,
                executeQueryBuffered);
            response.ScopeExecuteQuery.Add((int)Mode.PushBasedStreaming,
                executeQueryStreaming);

            response.SdkConnectionError.Add((int)Mode.Buffered,
                new SdkConnectionError
                {
                    InvalidCredErrorType = SdkConnectionError.Types
                        .InvalidCredentialErrorType
                        .AsInvalidCredentialException,
                    BootstrapErrorType = SdkConnectionError.Types
                        .BootstrapErrorType
                        .AsColumnarError
                });
            response.SdkConnectionError.Add((int)Mode.PushBasedStreaming,
                new SdkConnectionError
                {
                    InvalidCredErrorType = SdkConnectionError.Types
                        .InvalidCredentialErrorType
                        .AsInvalidCredentialException,
                    BootstrapErrorType = SdkConnectionError.Types
                        .BootstrapErrorType
                        .AsColumnarError
                });

            Serilog.Log.Information(
                "FetchPerformerCaps fetched {Seconds} seconds",
                stopWatch.Elapsed.TotalMilliseconds);
        }
        catch (Exception exception)
        {
            var statusCode = StatusCode.Aborted;
            if (exception is GrpcUnimplementedException)
            {
                statusCode = StatusCode.Unimplemented;
            }
            response.SdkConnectionError.Add((int)statusCode, new SdkConnectionError());
        }

        return Task.FromResult(response);
    }

    public override Task<EmptyResultOrFailureResponse> ClusterClose(ClusterCloseRequest request, ServerCallContext context)
    {
        Serilog.Log.Information("Calling ClusterClose for {ClusterId}", request.ExecutionContext.ClusterId);
        var stopWatch = LightweightStopwatch.StartNew();
        var initiated = Timestamp.FromDateTime(DateTime.UtcNow);

        var response = new EmptyResultOrFailureResponse();
        try
        {
            if (Clusters.TryRemove(request.ExecutionContext.ClusterId, out var connection))
            {
                connection.Dispose();
            }
            response.GetResponseMetaData(stopWatch, initiated);
        }
        catch (Exception ex)
        {
            response.GetResponseMetaData(stopWatch, initiated, ex);
            Serilog.Log.Error(ex, "Could not close cluster {ConnectionId}", request.ExecutionContext.ClusterId);
        }

        Serilog.Log.Information("Closing cluster {ClusterId} in {Milliseconds}ms", request.ExecutionContext.ClusterId, stopWatch.Elapsed.TotalMilliseconds);
        return Task.FromResult(response);
    }

    public override Task<EmptyResultOrFailureResponse> CloseAllClusters(CloseAllColumnarClustersRequest request,
        ServerCallContext context)
    {
        Serilog.Log.Information("Calling CloseAllClusters");
        var stopWatch = LightweightStopwatch.StartNew();
        var initiated = Timestamp.FromDateTime(DateTime.UtcNow);

        var response = new EmptyResultOrFailureResponse();
        var connectionIds = Clusters.Keys;

        try
        {
            foreach (var connection in connectionIds)
            {
                if (Clusters.TryRemove(connection, out var sdkCluster))
                {
                    Serilog.Log.Information("Closing cluster {ConnectionId}",
                        connection);
                    sdkCluster.Dispose();
                }
                else
                {
                    Serilog.Log.Information(
                        "Failed to remove cluster {ConnectionId}", connection);
                }
            }
            response.GetResponseMetaData(stopWatch, initiated);
        }
        catch (Exception ex)
        {
            response.GetResponseMetaData(stopWatch, initiated, ex);
        }

        Serilog.Log.Information("Closed all clusters in {Milliseconds}ms", stopWatch.Elapsed.TotalMilliseconds);
        return Task.FromResult(response);
    }

    public override Task<EchoResponse> Echo(EchoRequest request, ServerCallContext context)
    {
        Serilog.Log.Information("Calling Echo - {TestName} | {Message}", request.TestName, request.Message);
        return Task.FromResult(new EchoResponse());
    }
}