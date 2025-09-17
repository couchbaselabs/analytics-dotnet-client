using System.Net;
using Couchbase.AnalyticsClient.FunctionalTests.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace Couchbase.AnalyticsClient.FunctionalTests;

[Collection(TestCollection.Name)]
public class ClusterTests
{
    private readonly ITestOutputHelper _output;
    private readonly Analytics2Fixture _fixture;

    public ClusterTests(ITestOutputHelper output, Analytics2Fixture fixture)
    {
        _output = output;
        _fixture = fixture;
    }

    [Fact]
    public async Task Test_QueryAsync()
    {
        var cluster = _fixture.Cluster;

        using var response = await cluster.ExecuteQueryAsync("SELECT 1;", options => options.WithAsStreaming(true));

        Assert.NotNull(response);
    }
}