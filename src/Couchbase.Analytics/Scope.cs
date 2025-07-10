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

public sealed class Scope
{
    private readonly Cluster _cluster;
    private readonly Database _database;
    private readonly string _name;

    internal Scope(Database database, Cluster cluster, string name)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _cluster = cluster ?? throw new ArgumentNullException(nameof(cluster));
        _name = name ?? throw new ArgumentNullException(nameof(name));
    }

    public Task<IQueryResult<T>> ExecuteQueryAsync<T>(string statement, QueryOptions? options = null)
    {
        throw new NotImplementedException();
    }

    public Database Database(string databaseName)
    {
        throw new NotImplementedException();
    }
}