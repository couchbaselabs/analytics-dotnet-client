#region License
/* ************************************************************
 *
 *    @author Couchbase <info@couchbase.com>
 *    @copyright 2025 Couchbase, Inc.
 *
 *    Licensed under the Apache License, Version 2.0 (the "License");
 *    you may not use this file except in compliance with the License.
 *    You may obtain a copy of the License at
 *
 *        http://www.apache.org/licenses/LICENSE-2.0
 *
 *    Unless required by applicable law or agreed to in writing, software
 *    distributed under the License is distributed on an "AS IS" BASIS,
 *    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *    See the License for the specific language governing permissions and
 *    limitations under the License.
 *
 * ************************************************************/
#endregion

namespace Couchbase.AnalyticsClient.Internal.DnsUtil.Strategies;

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