using System.Net;
using System.Text;
using Couchbase.Analytics2.Exceptions;
using Couchbase.Analytics2.Internal;
using Couchbase.Analytics2.Internal.DI;
using Couchbase.Analytics2.Internal.HTTP;
using Couchbase.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;
using Xunit.Abstractions;

namespace Couchbase.Analytics2.UnitTests.Internal;

public class ClusterRetryTests
{
    private readonly ITestOutputHelper _outputHelper;
    private readonly Mock<ICouchbaseHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ILogger<Cluster>> _clusterLoggerMock;
    private readonly Mock<IDeserializer> _deserializerMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly string _connectionString;
    private readonly Credential _credential;

    public ClusterRetryTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
        _httpClientFactoryMock = new Mock<ICouchbaseHttpClientFactory>();
        _clusterLoggerMock = new Mock<ILogger<Cluster>>();
        _deserializerMock = new Mock<IDeserializer>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _connectionString = "http://127.0.0.1";
        _credential = new Credential("Administrator", "password");

        _httpClientFactoryMock.Setup(f => f.Create()).Returns(() => new HttpClient(_httpMessageHandlerMock.Object));
    }

    /// <summary>
    /// Tests that the cluster retries when it encounters *only* retriable errors, on a 200 OK response.
    /// Ultimately, the cluster should throw a QueryException with the last error in the list since it
    /// still doesn't succeed after all retries.
    /// </summary>
    [Fact]
    public async Task ExecuteQueryAsync_WithRetriableErrors_RetriesAndThrowsCorrectException()
    {
        const int maxRetries = 3;
        var cluster = CreateClusterWithRetryConfiguration(maxRetries);

        var retriableErrors = new List<Error>
        {
            new Error(24045, "Some unknown retriable error occurred", true),
            new Error(24055, "Another retriable error!", true)
        };

        var analyticsResultData = CreateAnalyticsResultData(retriableErrors);
        _deserializerMock.Setup(x => x.DeserializeAsync<AnalyticsResultData>(
                It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<AnalyticsResultData?>(analyticsResultData));

        // Configure HTTP handler to return 200 OK (the errors are in the deserialized response)
        var callCount = 0;
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns(() =>
            {
                callCount++;
                _outputHelper.WriteLine($"HTTP request attempt: {callCount}");

                // Return any valid JSON since we're mocking the deserializer
                var responseContent = new StringContent("{}", Encoding.UTF8, "application/json");
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = responseContent });
            });

        var exception = await Assert.ThrowsAsync<QueryException>(
            () => cluster.ExecuteQueryAsync("SELECT * FROM test", new QueryOptions { AsStreaming = false }));

        // Verify correct number of attempts (maxRetries + 1 attempts total, since we don't count the initial call as a "retry")
        Assert.Equal(maxRetries + 1, callCount);

        Assert.Equal(24045, exception.Code);
        Assert.Equal("Some unknown retriable error occurred", exception.ServerMessage);

        // We can verify the number of times an HTTP client was created
        _httpClientFactoryMock.Verify(f => f.Create(), Times.Exactly(maxRetries + 1));
    }

    /// <summary>
    /// Tests that the cluster does not retry when it encounters at least 1 non-retriable error, on a 200 OK response.
    /// Ultimately, the cluster should throw a QueryException with the first error in the list.
    /// </summary>
    [Fact]
    public async Task ExecuteQueryAsync_WithMixedErrors_DoesNotRetryAndThrowsNonRetriableError()
    {
        const int maxRetries = 3;
        var cluster = CreateClusterWithRetryConfiguration(maxRetries);

        // one non-retriable, one retriable
        var mixedErrors = new List<Error>
        {
            new Error(24001, "A non-retriable error occurred", false),
            new Error(24045, "Some retriable error", true)
        };

        var analyticsResultData = CreateAnalyticsResultData(mixedErrors);
        _deserializerMock.Setup(x => x.DeserializeAsync<AnalyticsResultData>(
                It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<AnalyticsResultData?>(analyticsResultData));

        var callCount = 0;
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns(() =>
            {
                callCount++;
                _outputHelper.WriteLine($"HTTP request attempt: {callCount}");

                var responseContent = new StringContent("{}", Encoding.UTF8, "application/json");
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = responseContent });
            });

        var exception = await Assert.ThrowsAsync<QueryException>(
            () => cluster.ExecuteQueryAsync("SELECT * FROM test", new QueryOptions { AsStreaming = false }));

        // Should not retry with mixed errors, only 1 attempt
        Assert.Equal(1, callCount);

        // Should throw for the first non-retriable error
        Assert.Equal(24001, exception.Code);
        Assert.Equal("A non-retriable error occurred", exception.ServerMessage);

        _httpClientFactoryMock.Verify(f => f.Create(), Times.Once);
    }

    /// <summary>
    /// Tests that the cluster retries when it encounters 1 retriable error, on a 200 OK response.
    /// Ultimately, the cluster should return a result on the second attempt.
    /// </summary>
    [Fact]
    public async Task ExecuteQueryAsync_WithSuccessfulRetry_ReturnsResultOnSecondAttempt()
    {
        const int maxRetries = 3;
        var cluster = CreateClusterWithRetryConfiguration(maxRetries);

        var callCount = 0;

        _deserializerMock.Setup(x => x.DeserializeAsync<AnalyticsResultData>(
                It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                if (callCount == 1)
                {
                    var retriableErrors = new List<Error>
                    {
                        new Error(24045, "Some unknown retriable error occurred", true)
                    };
                    return new ValueTask<AnalyticsResultData?>(CreateAnalyticsResultData(retriableErrors));
                }
                else
                {
                    // Second attempt: return success (no errors)
                    return new ValueTask<AnalyticsResultData?>(CreateAnalyticsResultData(new List<Error>()));
                }
            });

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns(() =>
            {
                callCount++;
                _outputHelper.WriteLine($"HTTP request attempt: {callCount}");

                var responseContent = new StringContent("{}", Encoding.UTF8, "application/json");
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = responseContent });
            });

        var result = await cluster.ExecuteQueryAsync("SELECT * FROM test", new QueryOptions { AsStreaming = false });

        Assert.NotNull(result);
        Assert.Equal(2, callCount);
        _httpClientFactoryMock.Verify(f => f.Create(), Times.Exactly(2));
    }

    /// <summary>
    /// Tests that the cluster does not retry when it encounters a retriable error, on a 200 OK response.
    /// Ultimately, the cluster should throw a QueryException with the first error in the list.
    /// </summary>
    [Fact]
    public async Task ExecuteQueryAsync_WithZeroRetries_DoesNotRetryOnRetriableError()
    {
        const int maxRetries = 0;
        var cluster = CreateClusterWithRetryConfiguration(maxRetries);

        var retriableErrors = new List<Error>
        {
            new Error(24045, "Some unknown retriable error occurred", true)
        };

        var analyticsResultData = CreateAnalyticsResultData(retriableErrors);
        _deserializerMock.Setup(x => x.DeserializeAsync<AnalyticsResultData>(
                It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<AnalyticsResultData?>(analyticsResultData));

        var callCount = 0;
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns(() =>
            {
                callCount++;
                _outputHelper.WriteLine($"HTTP request attempt: {callCount}");

                var responseContent = new StringContent("{}", Encoding.UTF8, "application/json");
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = responseContent });
            });

        var exception = await Assert.ThrowsAsync<QueryException>(
            () => cluster.ExecuteQueryAsync("SELECT * FROM test", new QueryOptions { AsStreaming = false }));

        Assert.Equal(1, callCount);
        Assert.Equal(24045, exception.Code);

        _httpClientFactoryMock.Verify(f => f.Create(), Times.Once);
    }

    private Cluster CreateClusterWithRetryConfiguration(int maxRetries)
    {
        var clusterOptions = new ClusterOptions { ConnectionString = _connectionString }
            .WithMaxRetries((uint)maxRetries)
            .WithTimeoutOptions(new TimeoutOptions().WithQueryTimeout(TimeSpan.FromSeconds(30)))
            .AddService<ICouchbaseHttpClientFactory, ICouchbaseHttpClientFactory>(
                _ => _httpClientFactoryMock.Object,
                ClusterServiceLifetime.Cluster)
            .AddService<ILogger<Cluster>, ILogger<Cluster>>(
                _ => _clusterLoggerMock.Object,
                ClusterServiceLifetime.Cluster)
            .AddService<IDeserializer, IDeserializer>(
                _ => _deserializerMock.Object,
                ClusterServiceLifetime.Cluster);

                return Cluster.Create(_credential, clusterOptions);
    }

    private static AnalyticsResultData CreateAnalyticsResultData(IReadOnlyList<Error> errors)
    {
        return new AnalyticsResultData
        {
            requestID = "test-request-id",
            status = errors.Count > 0 ? "fatal" : "success",
            results = new List<AnalyticsRow>(),
            errors = errors,
            metrics = new Metrics
            {
                elapsedTime = TimeSpan.FromMilliseconds(3.03758),
                executionTime = TimeSpan.FromMilliseconds(2.353572),
                compileTime = TimeSpan.FromMicroseconds(0.002),
                queueWaitTime = TimeSpan.Zero,
                resultCount = 0,
                resultSize = 0,
                processedObjects = 0,
                bufferCacheHitRatio = "0.00%"
            },
            plans = new Plans()
        };
    }
}