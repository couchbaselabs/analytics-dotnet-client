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
    public async Task FetchStatusAsync_DelegatesToService()
    {
        var serviceMock = new Mock<IAnalyticsService>();
        var handle = TestHandleFactory.CreateQueryHandle("test-handle", "test-req", "{}", serviceMock.Object);
        var expectedStatus = TestHandleFactory.CreateQueryStatus("test-req", """{"status":"running"}""", serviceMock.Object);

        serviceMock.Setup(x => x.FetchStatusAsync(handle, It.IsAny<FetchStatusOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedStatus);

        var result = await handle.FetchStatusAsync(new FetchStatusOptions());
        Assert.Same(expectedStatus, result);

        serviceMock.Verify(x => x.FetchStatusAsync(handle, It.IsAny<FetchStatusOptions>(), default), Times.Once);
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
    public async Task FetchStatusAsync_FluentOptions_DelegatesProperly()
    {
        var serviceMock = new Mock<IAnalyticsService>();
        var handle = TestHandleFactory.CreateQueryHandle("test-handle", "test-req", "{}", serviceMock.Object);
        var expectedStatus = TestHandleFactory.CreateQueryStatus("test-req", """{"status":"success","handle":"/result/path"}""", serviceMock.Object);

        serviceMock.Setup(x => x.FetchStatusAsync(handle, It.IsAny<FetchStatusOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedStatus);

        // Act using the fluent Options builder
        var result = await handle.FetchStatusAsync(opt => opt);

        Assert.Same(expectedStatus, result);
        serviceMock.Verify(x => x.FetchStatusAsync(handle, It.IsAny<FetchStatusOptions>(), default), Times.Once);
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
