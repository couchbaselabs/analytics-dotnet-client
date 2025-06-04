namespace Couchbase.Analytics2;

public record TimeoutOptions
{
    internal TimeSpan ConnectTimeout = TimeSpan.FromSeconds(10);
    internal TimeSpan DispatchTimeout = TimeSpan.FromSeconds(30);
    internal TimeSpan QueryTimeout = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Socket connection timeout, or more broadly the timeout
    /// for establishing an individual authenticated connection.
    /// <remarks>The default is 10s.</remarks>
    /// </summary>
    public TimeoutOptions WithConnectTimeout(TimeSpan connectTimeout )
    {
        ConnectTimeout = connectTimeout;
        return this;
    }

    /// <summary>
    /// How long the user is willing to wait for the SDK to retry
    /// a request due to network connectivity issues or unexpected
    /// cluster topology changes. Should be longer than the connect
    /// timeout, since recovery may involve multiple connection attempts.
    /// <remarks>The default is 30s.</remarks>
    /// </summary>
    public TimeoutOptions WithDispatchTimeout(TimeSpan dispatchTimeout )
    {
        DispatchTimeout = dispatchTimeout;
        return this;
    }

    /// <summary>
    /// Columnar query timeout.
    /// <remarks>The default is 10m.</remarks>
    /// </summary>
    public TimeoutOptions WithQueryTimeout(TimeSpan queryTimeout )
    {
        QueryTimeout = queryTimeout;
        return this;
    }
}