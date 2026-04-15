using System.Text.Json;
using Couchbase.AnalyticsClient.Async;
using Couchbase.AnalyticsClient.Internal;
using Couchbase.AnalyticsClient.Options;
using Couchbase.AnalyticsClient.UnitTests.Helpers;
using Moq;
using Xunit;

namespace Couchbase.AnalyticsClient.UnitTests.Async;

public class QueryHandleTests
{
    [Fact]
    public void Constructor_InitializesProperties()
    {
        var serviceMock = new Mock<IAnalyticsService>();
        var handle = TestHandleFactory.CreateQueryHandle("test-handle", "test-req", "{}", serviceMock.Object);

        Assert.Equal("test-handle", handle.Handle);
        Assert.Equal("test-req", handle.RequestId);
    }

    [Fact]
    public async Task FetchResultHandleAsync_DelegatesToService()
    {
        var serviceMock = new Mock<IAnalyticsService>();
        var handle = TestHandleFactory.CreateQueryHandle("test-handle", "test-req", "{}", serviceMock.Object);
        var expectedResult = TestHandleFactory.CreateQueryResultHandle("path", "req", "{}", serviceMock.Object);

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
        var handle = TestHandleFactory.CreateQueryHandle("test-handle", "test-req", "{}", serviceMock.Object);
        var options = new CancelOptions();

        serviceMock.Setup(x => x.CancelQueryAsync("test-req", options, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await handle.CancelAsync(options);

        serviceMock.Verify(x => x.CancelQueryAsync("test-req", options, default), Times.Once);
    }

    [Fact]
    public void Constructor_NullArguments_ThrowsArgumentNullException()
    {
        var serviceMock = new Mock<IAnalyticsService>();

        Assert.Throws<ArgumentNullException>(() => TestHandleFactory.CreateQueryHandle(null!, "req", "{}", serviceMock.Object));
        Assert.Throws<ArgumentNullException>(() => TestHandleFactory.CreateQueryHandle("handle", null!, "{}", serviceMock.Object));
        Assert.Throws<ArgumentException>(() => new QueryHandle("handle", "req", default(JsonElement), serviceMock.Object));
        Assert.Throws<ArgumentNullException>(() => TestHandleFactory.CreateQueryHandle("handle", "req", "{}", null!));
    }

    [Fact]
    public async Task FetchResultHandleAsync_FluentOptions_DelegatesProperly()
    {
        var serviceMock = new Mock<IAnalyticsService>();
        var handle = TestHandleFactory.CreateQueryHandle("test-handle", "test-req", "{}", serviceMock.Object);
        var expectedResult = TestHandleFactory.CreateQueryResultHandle("path", "req", "{}", serviceMock.Object);

        serviceMock.Setup(x => x.FetchResultHandleAsync(handle, It.IsAny<FetchResultHandleOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act using the fluent Options builder
        var result = await handle.FetchResultHandleAsync(opt => opt);

        Assert.Same(expectedResult, result);
        serviceMock.Verify(x => x.FetchResultHandleAsync(handle, It.IsAny<FetchResultHandleOptions>(), default), Times.Once);
    }

    [Fact]
    public async Task CancelAsync_FluentOptions_DelegatesProperly()
    {
        var serviceMock = new Mock<IAnalyticsService>();
        var handle = TestHandleFactory.CreateQueryHandle("test-handle", "test-req", "{}", serviceMock.Object);

        serviceMock.Setup(x => x.CancelQueryAsync("test-req", It.IsAny<CancelOptions>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act using the fluent Options builder
        await handle.CancelAsync(opt => opt);

        serviceMock.Verify(x => x.CancelQueryAsync("test-req", It.IsAny<CancelOptions>(), default), Times.Once);
    }
}
