using System.Net;
using Couchbase.Analytics2.Internal.HTTP;

namespace Couchbase.Analytics2.Internal;

internal interface IAnalyticsService
{
    HostEndpointWithPort EndPoint { get; }

    Task<IQueryResult<T>> SendAsync<T>(string statement, QueryOptions options);
}