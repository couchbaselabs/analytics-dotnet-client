namespace Couchbase.Analytics2;

public record TimeoutOptions
{
    private TimeSpan _connectTimeout = TimeSpan.FromSeconds(10);
    private TimeSpan _dispatchTimeout = TimeSpan.FromSeconds(30);
    private TimeSpan _queryTimeout = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Socket connection timeout, or more broadly the timeout
    /// for establishing an individual authenticated connection.
    /// <remarks>The default is 10s.</remarks>
    /// </summary>
    public TimeoutOptions ConnectTimeout(TimeSpan connectTimeout )
    {
        _connectTimeout = connectTimeout;
        return this;
    }

    /// <summary>
    /// How long the user is willing to wait for the SDK to retry
    /// a request due to network connectivity issues or unexpected
    /// cluster topology changes. Should be longer than the connect
    /// timeout, since recovery may involve multiple connection attempts.
    /// <remarks>The default is 30s.</remarks>
    /// </summary>
    public TimeoutOptions DispatchTimeout(TimeSpan dispatchTimeout )
    {
        _dispatchTimeout = dispatchTimeout;
        return this;
    }

    /// <summary>
    /// Columnar query timeout.
    /// <remarks>The default is 10m.</remarks>
    /// </summary>
    public TimeoutOptions QueryTimeout(TimeSpan queryTimeout )
    {
        _queryTimeout = queryTimeout;
        return this;
    }
}


