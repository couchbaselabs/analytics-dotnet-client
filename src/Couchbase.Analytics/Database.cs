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

public sealed class Database
{
    private readonly Cluster _cluster;
    private readonly string _databaseName;

    internal Database(Cluster cluster, string databaseName)
    {
         _cluster = cluster ?? throw new ArgumentNullException(nameof(cluster));
        _databaseName = databaseName ?? throw new ArgumentNullException(nameof(databaseName));
    }

    public string Name => _databaseName;

    public Scope Scope(string scopeName)
    {
        return new Scope(this, _cluster, scopeName);
    }

    public ScopeManager Scopes()
    {
        throw new NotImplementedException();
    }
}