using Couchbase.AnalyticsClient.Async;
using Couchbase.AnalyticsClient.Exceptions;
using Couchbase.AnalyticsClient.FunctionalTests.Fixtures;
using Couchbase.AnalyticsClient.Options;
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

        // 2. Poll for the query status
        QueryStatus? queryStatus = null;
        for (var i = 0; i < 20; i++)
        {
            queryStatus = await handle.FetchStatusAsync(new FetchStatusOptions());
            _outputHelper.WriteLine($"Status: {queryStatus}");
            if (queryStatus.ResultsReady)
            {
                break;
            }
            await Task.Delay(500);
        }

        Assert.NotNull(queryStatus);
        Assert.True(queryStatus!.ResultsReady);

        // 3. Get the result handle from the status
        var resultHandle = queryStatus.ResultHandle();
        Assert.NotNull(resultHandle);

        // 4. Fetch the results
        var results = await resultHandle.FetchResultsAsync(new FetchResultsOptions());
        Assert.NotNull(results);

        var count = 0;
        await foreach (var row in results.Rows)
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

        // Attempting to fetch the status afterwards should return 404 because the job is killed.
        // It's possible the cancel takes a brief moment to process gracefully on the server.
        var ex = await Record.ExceptionAsync(async () =>
        {
            for (var i = 0; i < 20; i++)
            {
                var queryStatus = await handle.FetchStatusAsync(new FetchStatusOptions());
                if (queryStatus.ResultsReady)
                {
                    // If it somehow completed, we're not testing cancellation properly, but let's break
                    break;
                }
                await Task.Delay(500);
            }
        });

        // The query should have been killed, resulting in a QueryNotFoundException when it's purged,
        // or a cleanly mapped QueryException ("Job Killed") if the server responds gracefully before purging.
        Assert.NotNull(ex);
        Assert.True(ex is QueryNotFoundException or QueryException,
            $"Expected QueryNotFoundException or QueryException upon cancellation, but received: {ex.GetType().FullName}");
    }

    [Fact]
    public async Task Test_AsyncAnalytics_DiscardResults_Cluster()
    {
        var statement = "select i from array_range(1, 5) as i;";
        var handle = await _simpleFixture.Cluster.StartQueryAsync(statement, new StartQueryOptions());

        // Poll for the query status
        QueryStatus? queryStatus = null;
        for (var i = 0; i < 20; i++)
        {
            queryStatus = await handle.FetchStatusAsync(new FetchStatusOptions());
            if (queryStatus.ResultsReady)
            {
                break;
            }
            await Task.Delay(500);
        }

        Assert.NotNull(queryStatus);
        Assert.True(queryStatus!.ResultsReady);

        var resultHandle = queryStatus.ResultHandle();
        Assert.NotNull(resultHandle);

        // Discard the results
        await resultHandle.DiscardResultsAsync(new DiscardResultsOptions());

        // Discarding again should succeed seamlessly due to 404 handling
        await resultHandle.DiscardResultsAsync(new DiscardResultsOptions());

        // Attempting to fetch the results after discarding should throw QueryNotFoundException
        await Assert.ThrowsAsync<QueryNotFoundException>(async () =>
        {
            await resultHandle.FetchResultsAsync(new FetchResultsOptions());
        });
    }
}
