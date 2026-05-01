using Couchbase.AnalyticsClient;
using Couchbase.AnalyticsClient.Async;
using Couchbase.AnalyticsClient.HTTP;
using Couchbase.AnalyticsClient.Options;
using Couchbase.AnalyticsClient.Results;
using Couchbase.Grpc.Protocol.Columnar;

namespace Couchbase.Analytics.Performer.Internal.Connections;

internal class ClusterConnection : IDisposable
{
    private readonly ClusterNewInstanceRequest _clusterNewInstanceRequest;
    private readonly Cluster _cluster;
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

    public Task<QueryHandle> StartClusterQuery(string statement, StartQueryOptions? options = null, CancellationToken cancellationToken = default)
    {
        return _cluster.StartQueryAsync(statement, options, cancellationToken);
    }

    public Task<QueryHandle> StartScopeQuery(string database, string scope, string statement, StartQueryOptions? options = null, CancellationToken cancellationToken = default)
    {
        return _cluster.Database(database).Scope(scope).StartQueryAsync(statement, options, cancellationToken);
    }

    public void UpdateCredential(ICredential credential)
    {
        _cluster.UpdateCredential(credential);
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
