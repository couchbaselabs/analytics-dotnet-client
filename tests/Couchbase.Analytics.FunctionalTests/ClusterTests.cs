using Couchbase.AnalyticsClient.FunctionalTests.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace Couchbase.AnalyticsClient.FunctionalTests;

[Collection(SimpleCollection.Name)]
public class ClusterTests
{
    private readonly ITestOutputHelper _output;
    private readonly SimpleFixture _fixture;

    public ClusterTests(ITestOutputHelper output, SimpleFixture fixture)
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
