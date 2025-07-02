namespace Couchbase.Analytics2.Internal.DnsUtil.Strategies;

internal interface IDnsRefreshStrategy
{
    bool ShouldRefreshDns();
    void OnDnsRefreshed();
    void OnRequest();
}