using System.Text.Json;
using Couchbase.Analytics2.Exceptions;
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
        var response = await _analytics2Fixture.Cluster.ExecuteQueryAsync("SELECT 1;", new QueryOptions());

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

    [Fact]
    public async Task Test_Streaming_Query()
    {
        var statement = "select i from array_range(1, 100) as i;";

        var result = await _analytics2Fixture.Cluster.ExecuteQueryAsync(statement,
            new QueryOptions() { Timeout = TimeSpan.FromSeconds(10), AsStreaming = true});

        await foreach (var row in result)
        {
            var value = row.ContentAs<JsonElement>();
            _outputHelper.WriteLine(value.ToString());
        }

        Assert.Equal(99, result.MetaData.Metrics.ResultCount);
    }

    [Fact]
    public async Task Test_Blocking_Query()
    {
        var statement = "select i from array_range(1, 100) as i;";

        var result = await _analytics2Fixture.Cluster.ExecuteQueryAsync(statement,
            new QueryOptions() { Timeout = TimeSpan.FromSeconds(10), AsStreaming = false});

        await foreach (var row in result)
        {
            var value = row.ContentAs<JsonElement>();
            _outputHelper.WriteLine(value.ToString());
        }

        Assert.Equal(99, result.MetaData.Metrics.ResultCount);
    }

    [Fact]
    public async Task Test_Cancellation_Works_Streaming()
    {
        var cts = new CancellationTokenSource();
        var statement = "select i from array_range(1, 100) as i;";
        await cts.CancelAsync();

        var task = _analytics2Fixture.Cluster.ExecuteQueryAsync(statement,
            new QueryOptions() { Timeout = TimeSpan.FromSeconds(10), AsStreaming = true},
            cts.Token);

        await Assert.ThrowsAsync<AnalyticsTimeoutException>(async () => await task.ConfigureAwait(false));
    }

    [Fact]
    public async Task Test_Cancellation_Works_Blocking()
    {
        var cts = new CancellationTokenSource();
        var statement = "select i from array_range(1, 100) as i;";
        await cts.CancelAsync();

        var task = _analytics2Fixture.Cluster.ExecuteQueryAsync(statement,
            new QueryOptions() { Timeout = TimeSpan.FromSeconds(10), AsStreaming = false},
            cts.Token);

        await Assert.ThrowsAsync<AnalyticsTimeoutException>(async () => await task.ConfigureAwait(false));
    }
}