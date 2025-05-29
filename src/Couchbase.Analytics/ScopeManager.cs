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
