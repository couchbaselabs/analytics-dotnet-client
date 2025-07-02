namespace Couchbase.Analytics2.Internal.DnsUtil.Strategies;

internal class CountBasedDnsRefreshStrategy : IDnsRefreshStrategy
{
    private readonly int _refreshRequestCount;
    private uint _requestCount;

    /// <summary>
    /// A DNS refresh strategy that refreshes the DNS record after a certain number of requests.
    /// By default (refreshRequestCount <= 1), the DNS record is refreshed for every request.
    /// </summary>
    public CountBasedDnsRefreshStrategy(int refreshRequestCount)
    {
        _refreshRequestCount = refreshRequestCount;
    }

    public bool ShouldRefreshDns()
    {
        // If parameter is 0 or 1, always get IPs from DNS record
        if (_refreshRequestCount <= 1)
        {
            return true;
        }

        return _requestCount > 0 && _requestCount % _refreshRequestCount == 0;
    }

    public void OnDnsRefreshed()
    {
        // Not needed for this strategy
    }

    public void OnRequest()
    {
        if (_refreshRequestCount > 1)
        {
            Interlocked.Increment(ref _requestCount);
        }
    }
}