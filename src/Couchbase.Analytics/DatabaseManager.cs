namespace Couchbase.Analytics2;

//https://docs.google.com/document/d/1BzNX8B5XqcDxn8NAi8lVrxP257IdG5kaOsA3mwDMLyQ/edit?tab=t.0#heading=h.chncuvne91d

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
