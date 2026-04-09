using Couchbase.AnalyticsClient;
using Couchbase.AnalyticsClient.Options;
using Couchbase.AnalyticsClient.Results;
using Couchbase.Grpc.Protocol.Columnar;

namespace Couchbase.Analytics.Performer.Internal.Connections;

internal class ClusterConnection : IDisposable
{
    private readonly ClusterNewInstanceRequest _clusterNewInstanceRequest;
    internal Cluster Cluster;
    private volatile bool _disposed;

    public ClusterConnection(
        ClusterNewInstanceRequest clusterNewInstanceRequest, Cluster cluster)
    {
        _clusterNewInstanceRequest = clusterNewInstanceRequest;
        Cluster = cluster;
    }

    public Task<IQueryResult> ExecuteClusterQuery(string statement, QueryOptions? options = null, CancellationToken? cancellationToken = null)
    {
        return Cluster.
            ExecuteQueryAsync(statement, options);
    }

    public Task<IQueryResult> ExecuteScopeQuery(string database, string scope, string statement, QueryOptions? options = null, CancellationToken? cancellationToken = null)
    {
        return Cluster.
            Database(database).
            Scope(scope).
            ExecuteQueryAsync(statement, options);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            Cluster?.Dispose();
        }
    }
}