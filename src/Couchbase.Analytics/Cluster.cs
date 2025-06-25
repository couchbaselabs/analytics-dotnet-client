using System.Collections.Concurrent;
using Couchbase.Analytics2.Internal;
using Couchbase.Analytics2.Internal.HTTP;
using Couchbase.Analytics2.Internal.Logging;
using Couchbase.Text.Json;
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

    private Cluster(Credential credential, ClusterOptions clusterOptions)
    {
        _credential = credential ?? throw new ArgumentNullException(nameof(credential));
        _clusterOptions = clusterOptions ?? throw new ArgumentNullException(nameof(clusterOptions));

        // Validate that connection string is provided
        if (string.IsNullOrWhiteSpace(_clusterOptions.ConnectionString))
        {
            throw new ArgumentException("ConnectionString cannot be null or empty.", nameof(clusterOptions));
        }

        _linkManager = new Lazy<LinkManager>(() => new LinkManager(this));
        _databaseManager = new Lazy<DatabaseManager>(() => new DatabaseManager(this));

        _analyticsService = new Lazy<IAnalyticsService>(() =>
        {
            var endpoint = _clusterOptions.ConnectionStringValue!.GetDnsBootStrapUri();
            var httpClientFactory = new CouchbaseHttpClientFactory(_credential, _clusterOptions, new Redactor(new TypedRedactor(RedactionLevel.None)), new NullLogger<CouchbaseHttpClientFactory>());
            var analyticsService = new AnalyticsService(_clusterOptions, httpClientFactory, endpoint, new NullLogger<AnalyticsService>(), new StjJsonDeserializer());

            return analyticsService;
        });
    }

    /// <summary>
    /// Creates a cluster with a connection string and credentials, allowing configuration of cluster options.
    /// </summary>
    /// <param name="connectionString">The connection string for the cluster</param>
    /// <param name="credential">The credentials to use for authentication</param>
    /// <param name="configureOptions">Action to configure cluster options</param>
    /// <returns>A Cluster instance</returns>
    /// <exception cref="ArgumentException">Thrown when the connection string is null or empty, or the credential is null</exception>
    public static Cluster Create(string connectionString, Credential credential, Action<ClusterOptions> configureOptions)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));

        ArgumentNullException.ThrowIfNull(credential);

        var options = new ClusterOptions
        {
            ConnectionString = connectionString
        };

        configureOptions.Invoke(options);
        return new Cluster(credential, options);
    }

    /// <summary>
    /// Creates a cluster with a connection string and credentials, allowing configuration of cluster options.
    /// </summary>
    /// <param name="connectionString">The connection string for the cluster</param>
    /// <param name="credential">The credentials to use for authentication</param>
    /// <param name="clusterOptions">The cluster options to use for the cluster</param>
    /// <returns>A Cluster instance</returns>
    /// <exception cref="ArgumentException">Thrown when the connection string is null or empty, or the credential is null</exception>
    public static Cluster Create(string connectionString, Credential credential, ClusterOptions clusterOptions){
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));

        ArgumentNullException.ThrowIfNull(credential);
        clusterOptions ??= new ClusterOptions();
        clusterOptions.ConnectionString = connectionString;

        return new Cluster(credential, clusterOptions);
    }

    /// <summary>
    /// Creates a cluster with a connection string and credentials, allowing configuration of cluster options.
    /// </summary>
    /// <param name="connectionString">The connection string for the cluster</param>
    /// <param name="credential">The credentials to use for authentication</param>
    /// <returns>A Cluster instance</returns>
    /// <exception cref="ArgumentException">Thrown when the connection string is null or empty, or the credential is null</exception>
    public static Cluster Create(string connectionString, Credential credential){
        return Create(connectionString, credential, (ClusterOptions?)null);
    }

    /// <summary>
    /// Creates a cluster with cluster options that must include a connection string.
    /// </summary>
    /// <param name="credential">The credentials to use for authentication</param>
    /// <param name="clusterOptions">Pre-configured cluster options with connection string</param>
    /// <returns>A Cluster instance</returns>
    /// <exception cref="ArgumentNullException">Thrown when the credential or cluster options are null</exception>
    public static Cluster Create(Credential credential, ClusterOptions clusterOptions)
    {
        ArgumentNullException.ThrowIfNull(credential);
        ArgumentNullException.ThrowIfNull(clusterOptions);

        return new Cluster(credential, clusterOptions);
    }

    public Task<IQueryResult<T>> ExecuteQueryAsync<T>(string statement, Action<QueryOptions> options)
    {
        var queryOptions = new QueryOptions();
        options.Invoke(queryOptions);
        return ExecuteQueryAsync<T>(statement, queryOptions);
    }

    public async Task<IQueryResult<T>> ExecuteQueryAsync<T>(string statement, QueryOptions? options = null)
    {
       var service = _analyticsService.Value;
       return await service.SendAsync<T>(statement, options ?? new QueryOptions()).ConfigureAwait(false);
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