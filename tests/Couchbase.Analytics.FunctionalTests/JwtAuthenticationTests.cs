using Couchbase.AnalyticsClient.Exceptions;
using Couchbase.AnalyticsClient.FunctionalTests.Fixtures;
using Couchbase.AnalyticsClient.HTTP;
using Couchbase.AnalyticsClient.Options;
using Xunit;
using Xunit.Abstractions;

namespace Couchbase.AnalyticsClient.FunctionalTests;

[Collection(JwtCollection.Name)]
public class JwtAuthenticationTests
{
    private readonly JwtFixture _fixture;
    private readonly ITestOutputHelper _output;

    public JwtAuthenticationTests(JwtFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    public async Task Test_Query_With_Jwt_Succeeds()
    {
        var result = await _fixture.Cluster.ExecuteQueryAsync(
            "SELECT 1;",
            new QueryOptions { Timeout = TimeSpan.FromSeconds(30) });

        Assert.NotNull(result);
        _output.WriteLine("JWT-authenticated query succeeded.");
    }

    [Fact]
    public async Task Test_UpdateCredential_With_Fresh_Jwt()
    {
        // The fixture's JWT is valid — use it to run a query first
        var result1 = await _fixture.Cluster.ExecuteQueryAsync(
            "SELECT 1;",
            new QueryOptions { Timeout = TimeSpan.FromSeconds(30) });
        Assert.NotNull(result1);

        // "Refresh" the credential with the same token (simulates a token rotation)
        var freshCredential = JwtCredential.Create(_fixture.FixtureSettings.JwtToken!);
        _fixture.Cluster.UpdateCredential(freshCredential);

        // Verify the cluster still works after the credential swap
        var result2 = await _fixture.Cluster.ExecuteQueryAsync(
            "SELECT 2;",
            new QueryOptions { Timeout = TimeSpan.FromSeconds(30) });
        Assert.NotNull(result2);

        // Restore the original credential so other tests aren't affected
        _fixture.Cluster.UpdateCredential(_fixture.JwtCredential);
        _output.WriteLine("UpdateCredential with fresh JWT succeeded.");
    }

    [Fact]
    public async Task Test_Query_With_Invalid_Jwt_Fails()
    {
        // Create a separate cluster with an obviously invalid JWT
        var invalidToken = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJleHBpcmVkIiwiZXhwIjoxfQ.invalid";
        var invalidCredential = JwtCredential.Create(invalidToken);
        using var invalidCluster = Cluster.Create(
            _fixture.FixtureSettings.ConnectionString!,
            invalidCredential,
            new ClusterOptions());

        await Assert.ThrowsAnyAsync<AnalyticsException>(async () =>
        {
            await invalidCluster.ExecuteQueryAsync(
                "SELECT 1 AS value;",
                new QueryOptions { Timeout = TimeSpan.FromSeconds(10) });
        });

        _output.WriteLine("Invalid JWT was correctly rejected by the server.");
    }
}
