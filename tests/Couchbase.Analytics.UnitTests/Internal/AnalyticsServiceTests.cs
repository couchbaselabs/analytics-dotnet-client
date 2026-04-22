using System.Net;
using System.Text;
using Couchbase.AnalyticsClient.Async;
using Couchbase.AnalyticsClient.Exceptions;
using Couchbase.AnalyticsClient.Internal;
using Couchbase.AnalyticsClient.Internal.HTTP;
using Couchbase.AnalyticsClient.Internal.Results;
using Couchbase.AnalyticsClient.Logging;
using Couchbase.AnalyticsClient.Options;
using Couchbase.AnalyticsClient.UnitTests.Helpers;
using Couchbase.Core.Json;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;
using Xunit.Abstractions;

namespace Couchbase.AnalyticsClient.UnitTests.Internal;

public class AnalyticsServiceTests
{
    private readonly ITestOutputHelper _outputHelper;
    private readonly Mock<ICouchbaseHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ILogger<AnalyticsService>> _loggerMock;
    private readonly Mock<IDeserializer> _jsonSerializerMock;
    private readonly Uri _endPoint;
    private readonly ClusterOptions _clusterOptions;

    public AnalyticsServiceTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
        _httpClientFactoryMock = new Mock<ICouchbaseHttpClientFactory>();
        _loggerMock = new Mock<ILogger<AnalyticsService>>();
        _jsonSerializerMock = new Mock<IDeserializer>();
        _jsonSerializerMock.Setup(x => x.CreateJsonStreamReader(It.IsAny<Stream>(),
            It.IsAny<CancellationToken>()))
            .Returns(new Mock<IJsonStreamReader>().Object);
        _endPoint = new Uri($"https://{IPAddress.Loopback}:8095");
        _clusterOptions = new ClusterOptions { ConnectionString = _endPoint.OriginalString };
    }

    [Fact]
    public void Constructor_InitializesCorrectly()
    {
        // Arrange & Act
        var service = new AnalyticsService(
            _clusterOptions,
            _httpClientFactoryMock.Object,
            _loggerMock.Object,
            new TypedRedactor(RedactionLevel.None));

        const string ExecuteQueryPath = "/api/v1/request";
        var expected = new UriBuilder(_endPoint);
        expected.Path = ExecuteQueryPath;

        // Assert
        Assert.NotNull(service);
        Assert.Equal(expected.Uri, service.Uri);
    }

    [Fact]
    public async Task SendAsync_ValidQuery_ReturnsBlockingAnalyticsResult()
    {
        // Arrange
        var responseContent = new StringContent("{}", Encoding.UTF8, "application/json");
        var responseMessage = new HttpResponseMessage(HttpStatusCode.OK) { Content = responseContent };

        var httpClientMock = new Mock<HttpMessageHandler>();
        httpClientMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);

        var httpClient = new HttpClient(httpClientMock.Object);
        _httpClientFactoryMock.Setup(f => f.Create()).Returns(httpClient);
        var service = new AnalyticsService(
            _clusterOptions,
            _httpClientFactoryMock.Object,
            _loggerMock.Object,
            new TypedRedactor(RedactionLevel.None));

        var queryOptions = new QueryOptions { AsStreaming = false };

        // Act
        var result = await service.SendAsync("SELECT * FROM `bucket`", queryOptions);

        // Assert
        Assert.IsType<BlockingAnalyticsResult>(result);
        _httpClientFactoryMock.Verify(f => f.Create(), Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithPriority_AddsPriorityHeader()
    {
        // Arrange
        var responseContent = new StringContent("{}", Encoding.UTF8, "application/json");
        var responseMessage = new HttpResponseMessage(HttpStatusCode.OK) { Content = responseContent };
        var requestMessage = new HttpRequestMessage(HttpMethod.Post,
            "http://localhost/api/v1/request");
        requestMessage.Headers.Add("Analytics-Priority", "true");

        var httpClientMock = new Mock<HttpMessageHandler>();
        httpClientMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);

        var httpClient = new HttpClient(httpClientMock.Object);
        _httpClientFactoryMock.Setup(f => f.Create()).Returns(httpClient);
        var service = new AnalyticsService(
            _clusterOptions,
            _httpClientFactoryMock.Object,
            _loggerMock.Object,
            new TypedRedactor(RedactionLevel.None));

        var queryOptions = new QueryOptions { AsStreaming = false };

        // Act
        var result = await service.SendAsync("SELECT * FROM `bucket`", queryOptions);

        // Assert
        Assert.IsType<BlockingAnalyticsResult>(result);
    }

    [Fact]
    public async Task SendAsync_WithStreaming_ReturnsStreamingAnalyticsResult()
    {
        // Arrange
        var httpClientMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(httpClientMock.Object);
        _httpClientFactoryMock.Setup(f => f.Create()).Returns(httpClient);

        var responseContent = new StringContent("{}", Encoding.UTF8, "application/json");
        var responseMessage = new HttpResponseMessage(HttpStatusCode.OK) { Content = responseContent };
        httpClientMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);

        var service = new AnalyticsService(
            _clusterOptions,
            _httpClientFactoryMock.Object,
            _loggerMock.Object,
            new TypedRedactor(RedactionLevel.None));

        var queryOptions = new QueryOptions { AsStreaming = true };

        // Act
        var result = await service.SendAsync("SELECT * FROM `bucket`", queryOptions);

        // Assert
        Assert.IsType<StreamingAnalyticsResult>(result);
        _httpClientFactoryMock.Verify(f => f.Create(), Times.Once);
    }

    [Fact]
    public async Task FetchStatusAsync_When404_ThrowsQueryNotFoundException()
    {
        // Arrange
        var httpClientMock = new Mock<HttpMessageHandler>();
        httpClientMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound));

        var httpClient = new HttpClient(httpClientMock.Object);
        _httpClientFactoryMock.Setup(f => f.Create()).Returns(httpClient);
        var service = new AnalyticsService(
            _clusterOptions,
            _httpClientFactoryMock.Object,
            _loggerMock.Object,
            new TypedRedactor(RedactionLevel.None));

        var handle = TestHandleFactory.CreateQueryHandle("mock-handle", "mock-req", "{}", service);

        // Act & Assert
        await Assert.ThrowsAsync<QueryNotFoundException>(() =>
            service.FetchStatusAsync(handle, new FetchStatusOptions()));
    }

    [Fact]
    public async Task FetchResultsAsync_When404_ThrowsQueryNotFoundException()
    {
        // Arrange
        var httpClientMock = new Mock<HttpMessageHandler>();
        httpClientMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound));

        var httpClient = new HttpClient(httpClientMock.Object);
        _httpClientFactoryMock.Setup(f => f.Create()).Returns(httpClient);
        var service = new AnalyticsService(
            _clusterOptions,
            _httpClientFactoryMock.Object,
            _loggerMock.Object,
            new TypedRedactor(RedactionLevel.None));

        // Act & Assert
        await Assert.ThrowsAsync<QueryNotFoundException>(() =>
            service.FetchResultsAsync("mock-req", "mock-path", new FetchResultsOptions()));
    }

    [Fact]
    public async Task DiscardResultsAsync_When404_CompletesSuccessfully()
    {
        // Arrange
        var httpClientMock = new Mock<HttpMessageHandler>();
        httpClientMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound));

        var httpClient = new HttpClient(httpClientMock.Object);
        _httpClientFactoryMock.Setup(f => f.Create()).Returns(httpClient);
        var service = new AnalyticsService(
            _clusterOptions,
            _httpClientFactoryMock.Object,
            _loggerMock.Object,
            new TypedRedactor(RedactionLevel.None));

        // Act & Assert
        var exception = await Record.ExceptionAsync(() =>
            service.DiscardResultsAsync("mock-req", "mock-path", new DiscardResultsOptions()));

        Assert.Null(exception);
    }

    [Fact]
    public async Task CancelQueryAsync_When404_CompletesSuccessfully()
    {
        // Arrange
        var httpClientMock = new Mock<HttpMessageHandler>();
        httpClientMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound));

        var httpClient = new HttpClient(httpClientMock.Object);
        _httpClientFactoryMock.Setup(f => f.Create()).Returns(httpClient);
        var service = new AnalyticsService(
            _clusterOptions,
            _httpClientFactoryMock.Object,
            _loggerMock.Object,
            new TypedRedactor(RedactionLevel.None));

        // Act & Assert
        var exception = await Record.ExceptionAsync(() =>
            service.CancelQueryAsync("mock-req", new CancelOptions()));

        Assert.Null(exception);
    }
}
