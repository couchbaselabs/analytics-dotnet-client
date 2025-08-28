using Couchbase.Analytics2.FunctionalTests.Fixtures;
using Xunit;
using DnsClient;
using Xunit.Abstractions;

namespace Couchbase.Analytics2.FunctionalTests.Internal;

[Collection(TestCollection.Name)]
public class AnalyticsServiceTests
{
    private readonly Analytics2Fixture _analytics2Fixture;
    private readonly ITestOutputHelper _outputHelper;

    public AnalyticsServiceTests(Analytics2Fixture analytics2Fixture, ITestOutputHelper outputHelper)
    {
        _analytics2Fixture = analytics2Fixture;
        _outputHelper = outputHelper;
    }

    [Fact]
    public async Task TestGetAnalyticsAsync()
    {
        var response = _analytics2Fixture.Cluster.ExecuteQueryAsync("SELECT 1;", new QueryOptions());

        Assert.NotNull(response);
    }

    [Fact]
    public async Task Test_DNSLookupAsync()
    {
        var lookupClient = new LookupClient();
        var hostname = _analytics2Fixture.FixtureSettings.ConnectionString!.Split(':')[0];
        var results = await lookupClient.QueryAsync(hostname, QueryType.A);
        Assert.NotNull(results);
    }
}