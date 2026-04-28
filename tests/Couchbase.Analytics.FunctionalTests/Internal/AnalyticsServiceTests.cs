using System.Text.Json;
using Couchbase.AnalyticsClient.Exceptions;
using Couchbase.AnalyticsClient.FunctionalTests.Fixtures;
using Couchbase.AnalyticsClient.Options;
using DnsClient;
using Xunit;
using Xunit.Abstractions;

namespace Couchbase.AnalyticsClient.FunctionalTests.Internal;

[Collection(SimpleCollection.Name)]
public class AnalyticsServiceTests
{
    private readonly SimpleFixture _simpleFixture;
    private readonly ITestOutputHelper _outputHelper;

    public AnalyticsServiceTests(SimpleFixture fixture, ITestOutputHelper outputHelper)
    {
        _simpleFixture = fixture;
        _outputHelper = outputHelper;
    }

    [Fact]
    public async Task TestGetAnalyticsAsync()
    {
        var response = await _simpleFixture.Cluster.ExecuteQueryAsync("SELECT 1;", new QueryOptions());

        Assert.NotNull(response);
    }

    [Fact]
    public async Task Test_DNSLookupAsync()
    {
        var lookupClient = new LookupClient();
        var hostname = _simpleFixture.FixtureSettings.ConnectionString!.Split(':')[0];
        var results = await lookupClient.QueryAsync(hostname, QueryType.A);
        Assert.NotNull(results);
    }

    [Fact]
    public async Task Test_Streaming_Query()
    {
        var statement = "select i from array_range(1, 100) as i;";

        await using var result = await _simpleFixture.Cluster.ExecuteQueryAsync(statement,
            new QueryOptions() { Timeout = TimeSpan.FromSeconds(10), AsStreaming = true });

        await foreach (var row in result.Rows)
        {
            var value = row.ContentAs<JsonElement>();
            try { _outputHelper.WriteLine(value.ToString()); }
            catch
            {
                // ignored
            }
        }

        Assert.Equal(99, result.MetaData.Metrics!.ResultCount);
    }

    [Fact]
    public async Task Test_Blocking_Query()
    {
        var statement = "select i from array_range(1, 100) as i;";

        await using var result = await _simpleFixture.Cluster.ExecuteQueryAsync(statement,
            new QueryOptions() { Timeout = TimeSpan.FromSeconds(10), AsStreaming = false });

        await foreach (var row in result.Rows)
        {
            var value = row.ContentAs<JsonElement>();
            try { _outputHelper.WriteLine(value.ToString()); }
            catch
            {
                //ignore
            }
        }

        Assert.Equal(99, result.MetaData.Metrics!.ResultCount);
    }

    [Fact]
    public async Task Test_Query_Metadata_And_Metrics()
    {
        try
        {
            _outputHelper.WriteLine(_simpleFixture.FixtureSettings
                .ConnectionString);
            _outputHelper.WriteLine(_simpleFixture.ClusterOptions
                .ConnectionString);
        }
        catch
        {
            //nada
        }

        var statement = "select i from array_range(1, 100) as i;";

        await using var result = await _simpleFixture.Cluster.ExecuteQueryAsync(statement,
            new QueryOptions() { Timeout = TimeSpan.FromSeconds(10), AsStreaming = false });

        Assert.NotNull(result.MetaData);
        Assert.NotNull(result.MetaData.Metrics);

        try
        {
            _outputHelper.WriteLine($"RequestId: {result.MetaData.RequestId}");
            _outputHelper.WriteLine($"ResultCount: {result.MetaData.Metrics.ResultCount}");
            _outputHelper.WriteLine($"ElapsedTime: {result.MetaData.Metrics.ElapsedTime}");
            _outputHelper.WriteLine($"ExecutionTime: {result.MetaData.Metrics.ExecutionTime}");
            _outputHelper.WriteLine($"ProcessedObjects: {result.MetaData.Metrics.ProcessedObjects}");
            _outputHelper.WriteLine($"ResultSize: {result.MetaData.Metrics.ResultSize}");
            _outputHelper.WriteLine($"CompileTime: {result.MetaData.Metrics.CompileTime}");
            _outputHelper.WriteLine($"QueueWaitTime: {result.MetaData.Metrics.QueueWaitTime}");
            _outputHelper.WriteLine($"BufferCacheHitRatio: {result.MetaData.Metrics.BufferCacheHitRatio}");
        }
        catch
        {
            //ignore
        }

        Assert.Equal(99, result.MetaData.Metrics.ResultCount);
        Assert.Equal(783, result.MetaData.Metrics.ResultSize);
        Assert.NotNull(result.MetaData.Metrics.ElapsedTime);
        Assert.NotNull(result.MetaData.Metrics.ExecutionTime);
        Assert.NotNull(result.MetaData.Metrics.CompileTime);
    }

    [Fact]
    public async Task Test_Cancellation_Works_Streaming()
    {
        var cts = new CancellationTokenSource();
        var statement = "select i from array_range(1, 100) as i;";
        await cts.CancelAsync();

        var task = _simpleFixture.Cluster.ExecuteQueryAsync(statement,
            new QueryOptions() { Timeout = TimeSpan.FromSeconds(10), AsStreaming = true },
            cts.Token);

        await Assert.ThrowsAsync<AnalyticsTimeoutException>(async () => await task.ConfigureAwait(false));
    }

    [Fact]
    public async Task Test_Cancellation_Works_Blocking()
    {
        var cts = new CancellationTokenSource();
        var statement = "select i from array_range(1, 100) as i;";
        await cts.CancelAsync();

        var task = _simpleFixture.Cluster.ExecuteQueryAsync(statement,
            new QueryOptions() { Timeout = TimeSpan.FromSeconds(10), AsStreaming = false },
            cts.Token);

        await Assert.ThrowsAsync<AnalyticsTimeoutException>(async () => await task.ConfigureAwait(false));
    }

    [Fact]
    public async Task Test_Streaming_Query_Scope()
    {
        var statement = "select i from array_range(1, 10) as i;";

        await using var result = await _simpleFixture.TestScope.ExecuteQueryAsync(statement,
            new QueryOptions() { Timeout = TimeSpan.FromSeconds(10), AsStreaming = true });

        var count = 0;
        await foreach (var row in result.Rows)
        {
            count++;
        }

        Assert.Equal(9, count);
        Assert.Equal(9, result.MetaData.Metrics!.ResultCount);
    }

    [Fact]
    public async Task Test_Blocking_Query_Scope()
    {
        var statement = "select i from array_range(1, 10) as i;";

        await using var result = await _simpleFixture.TestScope.ExecuteQueryAsync(statement,
            new QueryOptions() { Timeout = TimeSpan.FromSeconds(10), AsStreaming = false });

        var count = 0;
        await foreach (var row in result.Rows)
        {
            count++;
        }

        Assert.Equal(9, count);
        Assert.Equal(9, result.MetaData.Metrics!.ResultCount);
    }
}
