using System.Net;
using Couchbase.AnalyticsClient.Internal.DnsUtil;
using Couchbase.AnalyticsClient.Internal.DnsUtil.Strategies;
using Xunit;
using Xunit.Abstractions;

namespace Couchbase.AnalyticsClient.UnitTests.Internal.DnsUtil;

public class EndpointConnectionManagerTests
{
    private readonly ITestOutputHelper _outputHelper;

    public EndpointConnectionManagerTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }

    [Fact]
    public async Task EndpointConnectionManager_Should_Throw_AggregateException_If_All_Failed()
    {
        var manager = new EndpointConnectionManager(TimeSpan.MinValue);
        var addresses = new IPAddress[]
        {
            IPAddress.Parse("1.0.0.1"),
            IPAddress.Parse("1.0.0.2"),
            IPAddress.Parse("1.0.0.3")
        };

        var strategy = new RoundRobinEndpointSelector();
        await Assert.ThrowsAsync<AggregateException>( async () => await manager.ConnectToEndpointsAsync(addresses, 80, strategy, CancellationToken.None).ConfigureAwait(false));
    }
}