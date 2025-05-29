using System.Collections.Concurrent;
using Couchbase.Analytics2.Internal;

namespace Couchbase.Analytics2;

public class Cluster : IDisposable
{
    private readonly string _httpEndpoint;
    private readonly Credential _credential;
    private readonly ClusterOptions _clusterOptions;
    private readonly List<IAnalyticsService> _analyticsServices;
    private readonly ConcurrentDictionary<string, Database> _databases = new();
    private readonly Lazy<LinkManager> _linkManager;
    private readonly Lazy<DatabaseManager> _databaseManager;

    private Cluster(string httpEndpoint, Credential credential, ClusterOptions? clusterOptions = null)
    {
        _httpEndpoint = httpEndpoint ?? throw new ArgumentNullException(nameof(httpEndpoint));
        _credential = credential ?? throw new ArgumentNullException(nameof(credential));
        _clusterOptions = clusterOptions;
        _linkManager = new Lazy<LinkManager>(() => new LinkManager(this));
        _databaseManager = new Lazy<DatabaseManager>(() => new DatabaseManager(this));
        //_analyticsServices = new List<IAnalyticsService>() => new AnalyticsService(_clusterOptions);
    }

    public static Cluster Create(string httpEndpoint, Credential credential, ClusterOptions clusterOptions = null)
    {
        return new Cluster(httpEndpoint, credential, clusterOptions);
    }

    public Task<QueryResult<T>> ExecuteQueryAsync<T>(string statement, Action<QueryOptions> options)
    {
        var queryOptions = new QueryOptions();
        options?.Invoke(queryOptions);
        return ExecuteQueryAsync<T>(statement, queryOptions);
    }

    public Task<QueryResult<T>> ExecuteQueryAsync<T>(string statement, QueryOptions? options = null)
    {
        throw new NotImplementedException();
    }

    public Database Database(string databaseName)
    {
        return _databases.GetOrAdd(databaseName, database=> new Database(this, database));
    }

    public LinkManager Links()
    {
        return _linkManager.Value;
    }

    public DatabaseManager Databases()
    {
        return _databaseManager.Value;
    }

    public void Dispose()
    {
        // TODO release managed resources here
    }
}
