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

public sealed class ScopeManager
{
    private readonly Cluster _cluster;

    internal ScopeManager(Cluster cluster)
    {
        _cluster = cluster;
    }

    public Task<IEnumerable<ScopeMetaData>> GetAllScopesAsync(GetAllScopesOptions options)
    {
        throw new NotImplementedException();
    }

    public Task DropScopeAsync(string name, DropScopeOptions? options = null)
    {
        throw new NotImplementedException();
    }

    public Task CreateScopeAsync(string name, CreateScopeOptions? options = null)
    {
        throw new NotImplementedException();
    }
}

public record CreateScopeOptions
{
}

public record DropScopeOptions
{
}

public record GetAllScopesOptions
{
}

public record ScopeMetaData
{
}