#region License
/* ************************************************************
 *
 *    @author Couchbase <info@couchbase.com>
 *    @copyright 2025 Couchbase, Inc.
 *
 *    Licensed under the Apache License, Version 2.0 (the "License");
 *    you may not use this file except in compliance with the License.
 *    You may obtain a copy of the License at
 *
 *        http://www.apache.org/licenses/LICENSE-2.0
 *
 *    Unless required by applicable law or agreed to in writing, software
 *    distributed under the License is distributed on an "AS IS" BASIS,
 *    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *    See the License for the specific language governing permissions and
 *    limitations under the License.
 *
 * ************************************************************/
#endregion

using System.Collections.Concurrent;
using Couchbase.AnalyticsClient.Async;
using Couchbase.AnalyticsClient.HTTP;
using Couchbase.AnalyticsClient.Internal;
using Couchbase.AnalyticsClient.Internal.DI;
using Couchbase.AnalyticsClient.Options;
using Couchbase.AnalyticsClient.Results;
using Microsoft.Extensions.Logging;

namespace Couchbase.AnalyticsClient;

public class Cluster : IDisposable
{
    private readonly Credential _credential;
    private readonly ClusterOptions _clusterOptions;
    private readonly ILogger<Cluster> _logger;
    private readonly ICouchbaseServiceProvider _serviceProvider;
    private readonly LazyService<IAnalyticsService> _analyticsService;
    private readonly ConcurrentDictionary<string, Database> _databases = new();

    private Cluster(Credential credential, ClusterOptions clusterOptions)
    {
        if (string.IsNullOrWhiteSpace(clusterOptions.ConnectionString))
        {
            throw new ArgumentException("ConnectionString cannot be null or empty.", nameof(clusterOptions));
        }

        _credential = credential ?? throw new ArgumentNullException(nameof(credential));
        _clusterOptions = clusterOptions ?? throw new ArgumentNullException(nameof(clusterOptions));
        _serviceProvider = clusterOptions.BuildServiceProvider(_credential);

        _logger = _serviceProvider.GetRequiredService<ILogger<Cluster>>();
        _analyticsService = new LazyService<IAnalyticsService>(_serviceProvider);

    }

    /// <summary>
    /// Creates a cluster with a connection string and credentials, allowing configuration of cluster options.
    /// </summary>
    /// <param name="connectionString">The connection string for the cluster</param>
    /// <param name="credential">The credentials to use for authentication</param>
    /// <param name="configureOptions">Action to configure cluster options</param>
    /// <returns>A Cluster instance</returns>
    /// <exception cref="ArgumentException">Thrown when the connection string is null or empty, or the credential is null</exception>
    public static Cluster Create(string connectionString, Credential credential, Func<ClusterOptions, ClusterOptions> configureOptions)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));

        ArgumentNullException.ThrowIfNull(credential);

        var options = new ClusterOptions
        {
            ConnectionString = connectionString
        };

        options = configureOptions.Invoke(options);
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
    public static Cluster Create(string connectionString, Credential credential, ClusterOptions clusterOptions)
    {
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
    public static Cluster Create(string connectionString, Credential credential)
    {
        return Create(connectionString, credential, new ClusterOptions());
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

    public Task<IQueryResult> ExecuteQueryAsync(string statement, Func<QueryOptions, QueryOptions> options, CancellationToken cancellationToken = default)
    {
        var queryOptions = new QueryOptions();
        queryOptions = options.Invoke(queryOptions);
        return ExecuteQueryAsync(statement, queryOptions, cancellationToken);
    }

    public async Task<IQueryResult> ExecuteQueryAsync(string statement, QueryOptions? options = null, CancellationToken cancellationToken = default)
    {
        var service = _analyticsService.GetValueOrThrow();
        return await service.SendAsync(statement, options ?? new QueryOptions(), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Starts an asynchronous server-side query. The query is submitted to the server and a
    /// <see cref="QueryHandle"/> is returned that can be used to poll status, fetch results,
    /// cancel, or discard the query.
    /// </summary>
    /// <param name="statement">The analytics query statement to execute.</param>
    /// <param name="options">Options for the async query, including timeouts and result TTL.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="QueryHandle"/> for tracking and interacting with the query.</returns>
    public async Task<QueryHandle> StartQueryAsync(string statement, StartQueryOptions? options = null, CancellationToken cancellationToken = default)
    {
        var service = _analyticsService.GetValueOrThrow();
        return await service.StartQueryAsync(statement, options ?? new StartQueryOptions(), cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Starts an asynchronous server-side query with a fluent options builder.
    /// </summary>
    /// <param name="statement">The analytics query statement to execute.</param>
    /// <param name="options">A function to configure the <see cref="StartQueryOptions"/>.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="QueryHandle"/> for tracking and interacting with the query.</returns>
    public Task<QueryHandle> StartQueryAsync(string statement, Func<StartQueryOptions, StartQueryOptions> options, CancellationToken cancellationToken = default)
    {
        var startQueryOptions = new StartQueryOptions();
        startQueryOptions = options.Invoke(startQueryOptions);
        return StartQueryAsync(statement, startQueryOptions, cancellationToken);
    }

    /// <summary>
    /// Reconstructs a <see cref="QueryHandle"/> from a previously serialized handle string.
    /// This method does not perform any network operations.
    /// </summary>
    /// <param name="serializedHandle">A JSON string previously produced by <see cref="QueryHandle.Serialize"/>.</param>
    /// <returns>A <see cref="QueryHandle"/> that can be used to interact with the query.</returns>
    public QueryHandle QueryHandleFromSerialized(string serializedHandle)
    {
        var service = _analyticsService.GetValueOrThrow();
        return QueryHandle.Deserialize(serializedHandle, service);
    }

    public Database Database(string databaseName)
    {
        return _databases.GetOrAdd(databaseName, database => new Database(this, database));
    }

    public void Dispose()
    {
        // TODO release managed resources here
    }
}
