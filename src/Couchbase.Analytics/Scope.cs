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
