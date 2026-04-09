using Couchbase.AnalyticsClient.Async;
using Couchbase.AnalyticsClient.Internal;
using Couchbase.AnalyticsClient.Options;
using Couchbase.AnalyticsClient.Results;
using Moq;
using Xunit;

namespace Couchbase.AnalyticsClient.UnitTests.Async;

public class QueryHandleTests
{
    [Fact]
    public void Constructor_InitializesProperties()
    {
        var serviceMock = new Mock<IAnalyticsService>();
        var handle = new QueryHandle("test-handle", "test-req", serviceMock.Object);

        Assert.Equal("test-handle", handle.Handle);
        Assert.Equal("test-req", handle.RequestId);
    }

    [Fact]
    public async Task FetchResultHandleAsync_DelegatesToService()
    {
        var serviceMock = new Mock<IAnalyticsService>();
        var handle = new QueryHandle("test-handle", "test-req", serviceMock.Object);
        var expectedResult = new Mock<QueryResultHandle>("path", "req", serviceMock.Object).Object;

        serviceMock.Setup(x => x.FetchResultHandleAsync(handle, It.IsAny<FetchResultHandleOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var result = await handle.FetchResultHandleAsync(new FetchResultHandleOptions());
        Assert.Same(expectedResult, result);
        
        serviceMock.Verify(x => x.FetchResultHandleAsync(handle, It.IsAny<FetchResultHandleOptions>(), default), Times.Once);
    }

    [Fact]
    public async Task CancelAsync_DelegatesToService()
    {
        var serviceMock = new Mock<IAnalyticsService>();
        var handle = new QueryHandle("test-handle", "test-req", serviceMock.Object);
        var options = new CancelOptions();

        serviceMock.Setup(x => x.CancelQueryAsync("test-req", options, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await handle.CancelAsync(options);
        
        serviceMock.Verify(x => x.CancelQueryAsync("test-req", options, default), Times.Once);
    }
}
