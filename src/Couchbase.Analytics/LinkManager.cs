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
