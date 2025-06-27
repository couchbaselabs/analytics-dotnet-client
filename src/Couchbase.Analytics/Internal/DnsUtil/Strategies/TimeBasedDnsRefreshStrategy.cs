using System.Diagnostics;

namespace Couchbase.Analytics2.Internal.DnsUtil.Strategies;

internal class TimeBasedDnsRefreshStrategy : IDnsRefreshStrategy
{
    private readonly long _refreshIntervalTicks;
    private long _lastRefreshTimestamp;

    public TimeBasedDnsRefreshStrategy(TimeSpan refreshInterval)
    {
        _refreshIntervalTicks = (long)(refreshInterval.TotalSeconds * Stopwatch.Frequency);
        _lastRefreshTimestamp = Stopwatch.GetTimestamp();
    }

    public bool ShouldRefreshDns()
    {
        return Stopwatch.GetTimestamp() - Volatile.Read(ref _lastRefreshTimestamp) > _refreshIntervalTicks;
    }

    public void OnDnsRefreshed()
    {
        Volatile.Write(ref _lastRefreshTimestamp, Stopwatch.GetTimestamp());
    }

    public void OnRequest()
    {
        // No action needed for time-based strategy
    }
}