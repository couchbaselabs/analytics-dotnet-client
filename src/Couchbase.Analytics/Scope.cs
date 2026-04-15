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

using Couchbase.AnalyticsClient.Async;
using Couchbase.AnalyticsClient.Options;
using Couchbase.AnalyticsClient.Query;
using Couchbase.AnalyticsClient.Results;

namespace Couchbase.AnalyticsClient;

public sealed class Scope
{
    private readonly Cluster _cluster;
    private readonly Database _database;

    internal Scope(Database database, Cluster cluster, string name)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _cluster = cluster ?? throw new ArgumentNullException(nameof(cluster));
        if (string.IsNullOrEmpty(name)) throw new ArgumentException("Scope name cannot be null or empty", nameof(name));
        Name = name;
    }

    public string Name { get; }

    public Task<IQueryResult> ExecuteQueryAsync(string statement, QueryOptions? options = null, CancellationToken cancellationToken = default)
    {
        options ??= new QueryOptions();
        options = options with { QueryContext = new QueryContext(_database.Name, Name) };
        return _cluster.ExecuteQueryAsync(statement, options, cancellationToken);
    }

    public Task<IQueryResult> ExecuteQueryAsync(string statement, Func<QueryOptions, QueryOptions> options, CancellationToken cancellationToken = default)
    {
        var queryOptions = new QueryOptions().WithQueryContext(new QueryContext(_database.Name, Name));
        queryOptions = options.Invoke(queryOptions);
        return _cluster.ExecuteQueryAsync(statement, queryOptions, cancellationToken);
    }

    public Task<QueryHandle> StartQueryAsync(string statement, StartQueryOptions? options = null, CancellationToken cancellationToken = default)
    {
        options ??= new StartQueryOptions();
        options = options.WithQueryContext(new QueryContext(_database.Name, Name));
        return _cluster.StartQueryAsync(statement, options, cancellationToken);
    }

    public Task<QueryHandle> StartQueryAsync(string statement, Func<StartQueryOptions, StartQueryOptions> options, CancellationToken cancellationToken = default)
    {
        var startQueryOptions = new StartQueryOptions().WithQueryContext(new QueryContext(_database.Name, Name));
        startQueryOptions = options.Invoke(startQueryOptions);
        return _cluster.StartQueryAsync(statement, startQueryOptions, cancellationToken);
    }
}
