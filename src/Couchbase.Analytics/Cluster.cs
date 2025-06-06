using System.Collections.Concurrent;
using System.Net;
using Couchbase.Analytics2.Internal;
using Couchbase.Analytics2.Internal.HTTP;
using Couchbase.Analytics2.Internal.Logging;
using Couchbase.Analytics2.Internal.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Couchbase.Analytics2;

public class Cluster : IDisposable
{
    private readonly Credential _credential;
    private readonly ClusterOptions _clusterOptions;
    private readonly ConcurrentDictionary<string, Database> _databases = new();
    private readonly Lazy<LinkManager> _linkManager;
    private readonly Lazy<DatabaseManager> _databaseManager;
    private readonly Lazy<IAnalyticsService> _analyticsService;
    private readonly ConnectionString _connectionString;

    private Cluster(string httpEndpoint, Credential credential,
        ClusterOptions? clusterOptions = null)
    {
        _credential = credential ?? throw new ArgumentNullException(nameof(credential));
        _clusterOptions = clusterOptions ?? new ClusterOptions();
        _connectionString = ConnectionString.Parse(httpEndpoint);
        _linkManager = new Lazy<LinkManager>(() => new LinkManager(this));
        _databaseManager = new Lazy<DatabaseManager>(() => new DatabaseManager(this));

        _analyticsService = new Lazy<IAnalyticsService>(() =>
        {
            var endpoint = _connectionString.GetDnsBootStrapUri();
            var httpClientFactory = new CouchbaseHttpClientFactory(_credential, _clusterOptions.SecurityOptions, new Redactor(new TypedRedactor(RedactionLevel.None)), new NullLogger<CouchbaseHttpClientFactory>());
            var analyticsService = new AnalyticsService(_clusterOptions, httpClientFactory, endpoint, new NullLogger<AnalyticsService>(), new DefaultSerializer());

            return analyticsService;
        });
    }

    public static Cluster Create(string httpEndpoint, Credential credential, Action<ClusterOptions> clusterOptions)
    {
        var options = new ClusterOptions();
        clusterOptions?.Invoke(options);
        return new Cluster(httpEndpoint, credential, options);
    }

    public static Cluster Create(string httpEndpoint, Credential credential, ClusterOptions clusterOptions = null)
    {
        return new Cluster(httpEndpoint, credential, clusterOptions);
    }

    public Task<IQueryResult<T>> ExecuteQueryAsync<T>(string statement, Action<QueryOptions> options)
    {
        var queryOptions = new QueryOptions();
        options?.Invoke(queryOptions);
        return ExecuteQueryAsync<T>(statement, queryOptions);
    }

    public async Task<IQueryResult<T>> ExecuteQueryAsync<T>(string statement, QueryOptions? options = null)
    {
       var service = _analyticsService.Value;
       return await service.SendAsync<T>(statement, options ?? new QueryOptions());
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