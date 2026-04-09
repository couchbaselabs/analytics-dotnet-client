using Couchbase.AnalyticsClient.Async;
using Couchbase.AnalyticsClient.Internal;
using Couchbase.AnalyticsClient.Options;
using Couchbase.AnalyticsClient.Results;
using Moq;
using Xunit;

namespace Couchbase.AnalyticsClient.UnitTests.Async;

public class QueryResultHandleTests
{
    [Fact]
    public void Constructor_InitializesProperties()
    {
        var serviceMock = new Mock<IAnalyticsService>();
        var handle = new QueryResultHandle("test-path", "test-req", serviceMock.Object);

        Assert.Equal("test-req", handle.RequestId);
    }

    [Fact]
    public async Task FetchResultsAsync_DelegatesToService()
    {
        var serviceMock = new Mock<IAnalyticsService>();
        var handle = new QueryResultHandle("test-path", "test-req", serviceMock.Object);
        var expectedResult = new Mock<IQueryResult>().Object;
        var options = new FetchResultsOptions();

        serviceMock.Setup(x => x.FetchResultsAsync("test-req", "test-path", options, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var result = await handle.FetchResultsAsync(options);
        Assert.Same(expectedResult, result);
        
        serviceMock.Verify(x => x.FetchResultsAsync("test-req", "test-path", options, default), Times.Once);
    }

    [Fact]
    public async Task DiscardResultsAsync_DelegatesToService()
    {
        var serviceMock = new Mock<IAnalyticsService>();
        var handle = new QueryResultHandle("test-path", "test-req", serviceMock.Object);
        var options = new DiscardResultsOptions();

        serviceMock.Setup(x => x.DiscardResultsAsync("test-req", "test-path", options, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await handle.DiscardResultsAsync(options);
        
        serviceMock.Verify(x => x.DiscardResultsAsync("test-req", "test-path", options, default), Times.Once);
    }
}
