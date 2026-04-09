using System.Text.Json;
using Couchbase.AnalyticsClient.Exceptions;
using Couchbase.AnalyticsClient.FunctionalTests.Fixtures;
using Couchbase.AnalyticsClient.Options;
using Couchbase.AnalyticsClient.Async;
using Xunit;
using Xunit.Abstractions;

namespace Couchbase.AnalyticsClient.FunctionalTests;

[Collection(SimpleCollection.Name)]
public class AsyncAnalyticsTests
{
    private readonly SimpleFixture _simpleFixture;
    private readonly ITestOutputHelper _outputHelper;

    public AsyncAnalyticsTests(SimpleFixture fixture, ITestOutputHelper outputHelper)
    {
        _simpleFixture = fixture;
        _outputHelper = outputHelper;
    }

    [Fact]
    public async Task Test_AsyncAnalytics_EndToEnd_Cluster()
    {
        var statement = "select i from array_range(1, 100) as i;";
        var queryOptions = new StartQueryOptions()
        {
            QueryTimeout = TimeSpan.FromSeconds(30)
        };

        // 1. Start the query
        var handle = await _simpleFixture.Cluster.StartQueryAsync(statement, queryOptions);
        Assert.NotNull(handle);
        Assert.NotNull(handle.Handle);
        Assert.NotNull(handle.RequestId);
        
        _outputHelper.WriteLine($"Handle: {handle.Handle}");
        _outputHelper.WriteLine($"RequestId: {handle.RequestId}");

        // 2. Poll for the result handle
        QueryResultHandle? resultHandle = null;
        for (int i = 0; i < 20; i++)
        {
            resultHandle = await handle.FetchResultHandleAsync(new FetchResultHandleOptions());
            if (resultHandle != null)
            {
                break;
            }
            await Task.Delay(500);
        }

        Assert.NotNull(resultHandle);

        // 3. Fetch the results
        var results = await resultHandle!.FetchResultsAsync(new FetchResultsOptions());
        Assert.NotNull(results);

        var count = 0;
        await foreach (var row in results.ConfigureAwait(false))
        {
            count++;
        }

        Assert.Equal(99, count);
        Assert.Equal(99, results.MetaData.Metrics?.ResultCount);
    }

    [Fact]
    public async Task Test_AsyncAnalytics_Cancellation_Cluster()
    {
        // Use a statement that takes some time to execute (Cartesian product to delay)
        var statement = "select * from array_range(1, 5000) as a, array_range(1, 5000) as b;";
        var queryOptions = new StartQueryOptions()
        {
            QueryTimeout = TimeSpan.FromSeconds(30)
        };

        var handle = await _simpleFixture.Cluster.StartQueryAsync(statement, queryOptions);
        Assert.NotNull(handle);

        // Immediately cancel
        await handle.CancelAsync(new CancelOptions());

        // Attempting to fetch the handle afterwards should return 404 because the job is killed.
        // It's possible the cancel takes a brief moment to process gracefully on the server.
        var ex = await Record.ExceptionAsync(async () =>
        {
            for (int i = 0; i < 20; i++)
            {
                var resultHandle = await handle.FetchResultHandleAsync(new FetchResultHandleOptions());
                if (resultHandle != null)
                {
                    // If it somehow completed, we're not testing cancellation properly, but let's break
                    break;
                }
                await Task.Delay(500);
            }
        });

        // The query should have been killed, resulting in a QueryNotFoundException when it's purged.
        Assert.IsType<QueryNotFoundException>(ex);
    }

    [Fact]
    public async Task Test_AsyncAnalytics_DiscardResults_Cluster()
    {
        var statement = "select i from array_range(1, 5) as i;";
        var handle = await _simpleFixture.Cluster.StartQueryAsync(statement, new StartQueryOptions());

        // Poll for the result handle
        QueryResultHandle? resultHandle = null;
        for (int i = 0; i < 20; i++)
        {
            resultHandle = await handle.FetchResultHandleAsync(new FetchResultHandleOptions());
            if (resultHandle != null)
            {
                break;
            }
            await Task.Delay(500);
        }

        Assert.NotNull(resultHandle);

        // Discard the results
        await resultHandle!.DiscardResultsAsync(new DiscardResultsOptions());

        // Discarding again should succeed seamlessly due to 404 handling
        await resultHandle!.DiscardResultsAsync(new DiscardResultsOptions());

        // Attempting to fetch the results after discarding should throw QueryNotFoundException
        await Assert.ThrowsAsync<QueryNotFoundException>(async () => 
        {
            await resultHandle.FetchResultsAsync(new FetchResultsOptions());
        });
    }
}
