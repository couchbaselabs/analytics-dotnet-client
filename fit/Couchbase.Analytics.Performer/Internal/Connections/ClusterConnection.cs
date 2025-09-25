using Couchbase.AnalyticsClient;
using Couchbase.AnalyticsClient.Public;
using Couchbase.AnalyticsClient.Public.Options;
using Couchbase.AnalyticsClient.Public.Results;
using Couchbase.Grpc.Protocol.Columnar;

namespace Couchbase.Analytics.Performer.Internal.Connections;

internal class ClusterConnection : IDisposable
{
    private readonly ClusterNewInstanceRequest _clusterNewInstanceRequest;
    private Cluster _cluster;
    private volatile bool _disposed;

    public ClusterConnection(
        ClusterNewInstanceRequest clusterNewInstanceRequest, Cluster cluster)
    {
        _clusterNewInstanceRequest = clusterNewInstanceRequest;
        _cluster = cluster;
    }

    public Task<IQueryResult> ExecuteClusterQuery(string statement, QueryOptions? options = null, CancellationToken? cancellationToken = null)
    {
        return _cluster.
            ExecuteQueryAsync(statement, options);
    }

    public Task<IQueryResult> ExecuteScopeQuery(string database, string scope, string statement, QueryOptions? options = null, CancellationToken? cancellationToken = null)
    {
        return _cluster.
            Database(database).
            Scope(scope).
            ExecuteQueryAsync(statement, options);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _cluster?.Dispose();
        }
    }
}