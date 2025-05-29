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
