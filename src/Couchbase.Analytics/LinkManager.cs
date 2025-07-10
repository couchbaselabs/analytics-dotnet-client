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

public class LinkManager
{
    private readonly Cluster _cluster;

    internal LinkManager(Cluster cluster)
    {
        _cluster = cluster;
    }

    public Task CreateCouchbaseLinkAsync(CreateCouchbaseLinkSettings settings)
    {
        throw new NotImplementedException();
    }

    public Task UpdateCouchbaseLinkAsync(UpdateCouchbaseLinkSettings settings)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<CouchbaseLinkMetaData>> GetAllCouchbaseLinksAsync(GetAllCouchbaseLinksSettngs settings)
    {
        throw new NotImplementedException();
    }

    public Task CreateS3LinkAsync(CreateS3LinkSettings settings)
    {
        throw new NotImplementedException();
    }

    public Task UpdateS3LinkAsync(UpdateS3LinkSettings settings)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<S3LinkMetadata>> GetAllS3LinksAsync(GetAllS3LinksSettings settings)
    {
        throw new NotImplementedException();
    }

    public Task DropLinkAsync(string name, DropLinkOptions? options = null)
    {
        throw new NotImplementedException();
    }

    public Task ConnectLinkAsync(ConnectLinkOptions? options = null)
    {
        throw new NotImplementedException();
    }

    public Task DisconnectLinkAsync(DisconnectLinkOptions? options = null)
    {
        throw new NotImplementedException();
    }
}

public record DisconnectLinkOptions
{
}

public record ConnectLinkOptions
{
}

public record DropLinkOptions
{
}

public record S3LinkMetadata
{
}

public record GetAllS3LinksSettings
{
}

public record CouchbaseLinkMetaData
{
}

public record UpdateS3LinkSettings
{
}

public record CreateS3LinkSettings
{
}

public record GetAllCouchbaseLinksSettngs
{
}

public record UpdateCouchbaseLinkSettings
{
}

public record CreateCouchbaseLinkSettings
{
}