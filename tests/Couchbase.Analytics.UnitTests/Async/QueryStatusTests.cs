using System.Text.Json;
using Couchbase.AnalyticsClient.Async;
using Couchbase.AnalyticsClient.Internal;
using Couchbase.AnalyticsClient.UnitTests.Helpers;
using Moq;
using Xunit;

namespace Couchbase.AnalyticsClient.UnitTests.Async;

public class QueryStatusTests
{
    [Fact]
    public void ResultsReady_WhenSuccess_ReturnsTrue()
    {
        var serviceMock = new Mock<IAnalyticsService>();
        var json = """
        {
            "status": "success",
            "handle": "/api/v1/request/result/abc/12-0",
            "resultCount": 100
        }
        """;

        var status = TestHandleFactory.CreateQueryStatus("req-1", json, serviceMock.Object);

        Assert.True(status.ResultsReady);
    }

    [Theory]
    [InlineData("running")]
    [InlineData("queued")]
    public void ResultsReady_WhenNotSuccess_ReturnsFalse(string serverStatus)
    {
        var serviceMock = new Mock<IAnalyticsService>();
        var json = $$"""{"status": "{{serverStatus}}"}""";

        var status = TestHandleFactory.CreateQueryStatus("req-1", json, serviceMock.Object);

        Assert.False(status.ResultsReady);
    }

    [Fact]
    public void ResultHandle_WhenReady_ReturnsQueryResultHandle()
    {
        var serviceMock = new Mock<IAnalyticsService>();
        var json = """
        {
            "status": "success",
            "handle": "/api/v1/request/result/abc/12-0",
            "resultCount": 50
        }
        """;

        var status = TestHandleFactory.CreateQueryStatus("req-1", json, serviceMock.Object);
        var resultHandle = status.ResultHandle();

        Assert.NotNull(resultHandle);
        Assert.Equal("req-1", resultHandle.RequestId);
    }

    [Fact]
    public void ResultHandle_WhenNotReady_ThrowsInvalidOperationException()
    {
        var serviceMock = new Mock<IAnalyticsService>();
        var json = """{"status": "running"}""";

        var status = TestHandleFactory.CreateQueryStatus("req-1", json, serviceMock.Object);

        var ex = Assert.Throws<InvalidOperationException>(() => status.ResultHandle());
        Assert.Contains("Results are not ready", ex.Message);
    }

    [Fact]
    public void ToString_IncludesAllAvailableFields()
    {
        var serviceMock = new Mock<IAnalyticsService>();
        var json = """
        {
            "status": "success",
            "handle": "/api/v1/request/result/abc/12-0",
            "resultCount": 100,
            "resultSetOrdered": false,
            "createdAt": "2026-03-16T17:15:40.850Z",
            "partitions": [
                { "handle": "/api/v1/request/result/abc/12-0", "resultCount": 100 }
            ],
            "metrics": {
                "elapsedTime": "607.586ms",
                "executionTime": "50.195s",
                "compileTime": "11.792ms",
                "queueWaitTime": "0ns",
                "processedObjects": 42
            }
        }
        """;

        var status = TestHandleFactory.CreateQueryStatus("req-1", json, serviceMock.Object);
        var str = status.ToString();

        Assert.Contains("Status=success", str);
        Assert.Contains("ResultsReady=True", str);
        Assert.Contains("ResultCount=100", str);
        Assert.Contains("ResultSetOrdered=False", str);
        Assert.Contains("Partitions=1", str);
        Assert.Contains("CreatedAt=2026-03-16T17:15:40.850Z", str);
        Assert.Contains("ElapsedTime=", str);
        Assert.Contains("ProcessedObjects=42", str);
    }

    [Fact]
    public void ToString_WithMetricsOnly_OmitsMissingFields()
    {
        var serviceMock = new Mock<IAnalyticsService>();
        var json = """
        {
            "status": "running",
            "metrics": {
                "elapsedTime": "10.582s",
                "executionTime": "10.477s",
                "compileTime": "105ms",
                "queueWaitTime": "0ns"
            }
        }
        """;

        var status = TestHandleFactory.CreateQueryStatus("req-1", json, serviceMock.Object);
        var str = status.ToString();

        Assert.Contains("Status=running", str);
        Assert.Contains("ResultsReady=False", str);
        Assert.Contains("Metrics=", str);
        // These fields should not appear since they're absent from JSON
        Assert.DoesNotContain("ResultCount", str);
        Assert.DoesNotContain("ResultSetOrdered", str);
        Assert.DoesNotContain("Partitions", str);
        Assert.DoesNotContain("CreatedAt", str);
        Assert.DoesNotContain("ProcessedObjects", str);
    }

    [Fact]
    public void ToString_MinimalResponse_NoMetrics()
    {
        var serviceMock = new Mock<IAnalyticsService>();
        var json = """{"status": "queued"}""";

        var status = TestHandleFactory.CreateQueryStatus("req-1", json, serviceMock.Object);
        var str = status.ToString();

        Assert.Contains("Status=queued", str);
        // With no metrics at all, the Metrics section should be absent
        Assert.DoesNotContain("Metrics=", str);
        Assert.DoesNotContain("ResultCount", str);
    }

    [Fact]
    public void ToString_CreatedAtOnly_IncludesTimestamp()
    {
        var serviceMock = new Mock<IAnalyticsService>();
        var json = """
        {
            "status": "running",
            "createdAt": "2026-04-15T23:22:19.246Z"
        }
        """;

        var status = TestHandleFactory.CreateQueryStatus("req-1", json, serviceMock.Object);
        var str = status.ToString();

        Assert.Contains("CreatedAt=2026-04-15T23:22:19.246Z", str);
    }

    [Fact]
    public void Constructor_NullArguments_ThrowsArgumentNullException()
    {
        var serviceMock = new Mock<IAnalyticsService>();

        Assert.Throws<ArgumentNullException>(() => TestHandleFactory.CreateQueryStatus(null!, "{}", serviceMock.Object));
        Assert.Throws<ArgumentException>(() => new QueryStatus("req", default(JsonElement), serviceMock.Object));
        Assert.Throws<ArgumentNullException>(() => TestHandleFactory.CreateQueryStatus("req", "{}", null!));
    }

    [Fact]
    public void InternalProperties_ParsedCorrectly()
    {
        var serviceMock = new Mock<IAnalyticsService>();
        var json = """
        {
            "status": "success",
            "handle": "/api/v1/request/result/abc/12-0",
            "resultCount": 42,
            "resultSetOrdered": true,
            "createdAt": "2026-03-16T17:15:40.850Z",
            "partitions": [
                { "handle": "/p1", "resultCount": 20 },
                { "handle": "/p2", "resultCount": 22 }
            ],
            "metrics": {
                "processedObjects": 999
            }
        }
        """;

        var status = TestHandleFactory.CreateQueryStatus("req-1", json, serviceMock.Object);

        Assert.Equal("success", status.Status);
        Assert.Equal(42, status.ResultCount);
        Assert.True(status.ResultSetOrdered);
        Assert.Equal("2026-03-16T17:15:40.850Z", status.CreatedAt);
        Assert.Equal(2, status.PartitionCount);
        Assert.Equal(999, status.Metrics?.ProcessedObjects);
    }

    [Fact]
    public void InternalProperties_MissingFields_DefaultGracefully()
    {
        var serviceMock = new Mock<IAnalyticsService>();
        var json = """{"status": "running"}""";

        var status = TestHandleFactory.CreateQueryStatus("req-1", json, serviceMock.Object);

        Assert.Equal("running", status.Status);
        Assert.Null(status.ResultCount);
        Assert.Null(status.ResultSetOrdered);
        Assert.Null(status.CreatedAt);
        Assert.Equal(0, status.PartitionCount);
        Assert.Null(status.Metrics);
    }
}
