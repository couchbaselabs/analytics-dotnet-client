using System.Net;
using Couchbase.Analytics2.FunctionalTests.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace Couchbase.Analytics2.FunctionalTests;

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
        var cluster = Cluster.Create("5e07bed7-20250516.cb-sdk.bemdas.com:8095",
            new Credential("Administrator", "password"),
            new ClusterOptions()
            {
                SecurityOptions = new SecurityOptions().WithTrustOnlyPemFile(_fixture.CapellaCaCert)
            });

        using var response = await cluster.ExecuteQueryAsync<dynamic>("SELECT 1;", options =>
        {
            options.AsStreaming = false;
        });

        Assert.NotNull(response);
    }
}