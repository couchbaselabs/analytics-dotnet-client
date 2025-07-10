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
namespace Couchbase.Analytics2;

public class DatabaseManager
{
    private readonly Cluster _cluster;

    internal DatabaseManager(Cluster cluster)
    {
        _cluster = cluster;
    }

    public Task<IEnumerable<DatabaseMetaData>> GetAllDatabasesAsync(GetAllDatabaseOptions options = null)
    {
        throw new NotImplementedException();
    }

    public Task DropDatabaseAsync(DropDatabaseOptions options = null)
    {
        throw new NotImplementedException();
    }

    public Task CreateDatabaseAsync(string databaseName, CreateDatabaseOptions options = null)
    {
        throw new NotImplementedException();
    }
}

public record CreateDatabaseOptions
{
    /// <summary>
    /// The maximum amount of time that the request is allowed to take.
    /// </summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// When set to true the SDK must suffix the drop database query with `if exists`.
    /// </summary>
    /// <remarks>Default is false.</remarks>
    public bool IgnoreIfNotExists { get; init; } = false;
}

public class DropDatabaseOptions
{
    /// <summary>
    /// The maximum amount of time that the request is allowed to take.
    /// </summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// When set to true the SDK must suffix the drop database query with `if exists`.
    /// </summary>
    /// <remarks>Default is false.</remarks>
    public bool IgnoreIfNotExists { get; init; } = false;
}

public record DatabaseMetaData
{
    /// <summary>
    /// The name of the database.
    /// </summary>
   public string Database { get; init; } = string.Empty;

    /// <summary>
    /// `true` if the database is a system database.
    /// </summary>
   public bool IsSystemDatabase { get; init; } = false;
}

public record GetAllDatabaseOptions
{
    /// <summary>
    /// The maximum amount of time that the request is allowed to take.
    /// </summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(1);
}