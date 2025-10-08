using System.Net;
using System.Text;
using System.Text.Json;
using Couchbase.AnalyticsClient.DI;
using Couchbase.AnalyticsClient.Exceptions;
using Couchbase.AnalyticsClient.HTTP;
using Couchbase.AnalyticsClient.Internal.HTTP;
using Couchbase.AnalyticsClient.Options;
using Couchbase.AnalyticsClient.Query;
using Couchbase.Core.Json;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;
using Xunit.Abstractions;

namespace Couchbase.AnalyticsClient.UnitTests.Internal;

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

        var retriableErrors = new List<QueryError>
        {
            new QueryError(24045, "Some unknown retriable error occurred", true),
            new QueryError(24055, "Another retriable error!", true)
        };

        // Configure HTTP handler to return 200 OK with retriable errors in the body
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

                var responseJson = BuildErrorResponseJson(retriableErrors);
                var responseContent = new StringContent(responseJson, Encoding.UTF8, "application/json");
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = responseContent });
            });

        var exception = await Assert.ThrowsAsync<QueryException>(
            () => cluster.ExecuteQueryAsync("SELECT * FROM test", new QueryOptions { AsStreaming = false }));

        // Verify correct number of attempts (maxRetries + 1 attempts total, since we don't count the initial call as a "retry")
        Assert.Equal(maxRetries + 1, callCount);

        Assert.Equal(24045, exception.Code);
        Assert.Equal("Some unknown retriable error occurred", exception.ServerMessage);

        // We're verifying the number of times an HTTP client was created is exactly 1,
        // since we now reuse the same HttpClient instance across retries.
        _httpClientFactoryMock.Verify(f => f.Create(), Times.Once);
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
        var mixedErrors = new List<QueryError>
        {
            new QueryError(24001, "A non-retriable error occurred", false),
            new QueryError(24045, "Some retriable error", true)
        };

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

                var responseJson = BuildErrorResponseJson(mixedErrors);
                var responseContent = new StringContent(responseJson, Encoding.UTF8, "application/json");
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

                if (callCount == 1)
                {
                    var retriableErrors = new List<QueryError>
                    {
                        new QueryError(24045, "Some unknown retriable error occurred", true)
                    };
                    var responseJson = BuildErrorResponseJson(retriableErrors);
                    var responseContent = new StringContent(responseJson, Encoding.UTF8, "application/json");
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = responseContent });
                }
                else
                {
                    var responseJson = BuildSuccessResponseJson();
                    var responseContent = new StringContent(responseJson, Encoding.UTF8, "application/json");
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = responseContent });
                }
            });

        var result = await cluster.ExecuteQueryAsync("SELECT * FROM test", new QueryOptions { AsStreaming = false });

        Assert.NotNull(result);
        Assert.Equal(2, callCount);

        // We're verifying the number of times an HTTP client was created is exactly 1,
        // since we now reuse the same HttpClient instance across retries.
        _httpClientFactoryMock.Verify(f => f.Create(), Times.Once);
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

        var retriableErrors = new List<QueryError>
        {
            new QueryError(24045, "Some unknown retriable error occurred", true)
        };

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

                var responseJson = BuildErrorResponseJson(retriableErrors);
                var responseContent = new StringContent(responseJson, Encoding.UTF8, "application/json");
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
                _ => new StjJsonDeserializer(),
                ClusterServiceLifetime.Cluster);

                return Cluster.Create(_credential, clusterOptions);
    }

    private static string BuildErrorResponseJson(IEnumerable<QueryError> errors)
    {
        var payload = new
        {
            status = "fatal",
            errors = errors.Select(e => new { code = e.Code, msg = e.Message, retriable = e.Retriable })
        };

        return JsonSerializer.Serialize(payload);
    }

    private static string BuildSuccessResponseJson()
    {
        return "{\"status\":\"success\",\"results\":[]}";
    }
}