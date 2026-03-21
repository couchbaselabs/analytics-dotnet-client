using System.Text.Json;
using Couchbase.AnalyticsClient.Async;
using Couchbase.AnalyticsClient.Exceptions;
using Couchbase.AnalyticsClient.Internal;
using Couchbase.AnalyticsClient.Options;
using Couchbase.AnalyticsClient.Results;
using Couchbase.Core.Json;
using Xunit;

namespace Couchbase.AnalyticsClient.UnitTests.Async;

public class QueryStatusTests
{
    private static readonly IAnalyticsService StubService = new StubAnalyticsService();
    private static readonly IDeserializer StubDeserializer = new StjJsonDeserializer(new JsonSerializerOptions());

    private static QueryStatus Create(string status, string? resultHandle = null) =>
        new(status, resultHandle, errors: null, metrics: null, StubService, StubDeserializer);

    // ─── AreResultsReady ───

    [Fact]
    public void AreResultsReady_Success_ReturnsTrue()
    {
        Assert.True(Create("success", "/api/v1/request/result/abc/1-0").AreResultsReady);
    }

    [Theory]
    [InlineData("queued")]
    [InlineData("running")]
    [InlineData("fatal")]
    [InlineData("failed")]
    [InlineData("timeout")]
    public void AreResultsReady_NonSuccess_ReturnsFalse(string status)
    {
        Assert.False(Create(status).AreResultsReady);
    }

    // ─── IsError ───

    [Theory]
    [InlineData("fatal")]
    [InlineData("timeout")]
    [InlineData("failed")]  // undocumented but observed from server (e.g., cancelled queries)
    public void IsError_TerminalErrorStatuses_ReturnsTrue(string status)
    {
        Assert.True(Create(status).IsError);
    }

    [Theory]
    [InlineData("queued")]
    [InlineData("running")]
    [InlineData("success")]
    public void IsError_NonErrorStatuses_ReturnsFalse(string status)
    {
        Assert.False(Create(status).IsError);
    }

    // ─── Case insensitivity ───
    // One test proves all status comparisons are case-insensitive by checking
    // every known status in UPPER and Title case against its lowercase behavior.

    [Theory]
    [InlineData("success")]
    [InlineData("fatal")]
    [InlineData("failed")]
    [InlineData("timeout")]
    [InlineData("queued")]
    [InlineData("running")]
    public void StatusComparisons_AreCaseInsensitive(string lowercase)
    {
        var upper = Create(lowercase.ToUpperInvariant());
        var title = Create(char.ToUpperInvariant(lowercase[0]) + lowercase[1..]);
        var lower = Create(lowercase);

        Assert.Equal(lower.AreResultsReady, upper.AreResultsReady);
        Assert.Equal(lower.AreResultsReady, title.AreResultsReady);
        Assert.Equal(lower.IsError, upper.IsError);
        Assert.Equal(lower.IsError, title.IsError);
    }

    // ─── GetResults ───

    [Fact]
    public void GetResults_WhenSuccess_ReturnsHandleResults()
    {
        var (results, error) = Create("success", "/api/v1/request/result/abc/1-0").GetResults();
        Assert.NotNull(results);
        Assert.Null(error);
    }

    [Theory]
    [InlineData("fatal")]
    [InlineData("failed")]
    [InlineData("timeout")]
    public void GetResults_WhenError_ReturnsException(string status)
    {
        var (results, error) = Create(status).GetResults();
        Assert.Null(results);
        Assert.IsAssignableFrom<AnalyticsException>(error);
    }

    [Theory]
    [InlineData("queued")]
    [InlineData("running")]
    public void GetResults_WhenInProgress_ThrowsInvalidOperation(string status)
    {
        Assert.Throws<InvalidOperationException>(() => Create(status).GetResults());
    }

    // ─── Stub service ───

    private sealed class StubAnalyticsService : IAnalyticsService
    {
        public Uri Uri { get; } = new("http://localhost");

        public Task<IQueryResult> SendAsync(string statement, QueryOptions options, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<QueryHandle> StartQueryAsync(string statement, StartQueryOptions options, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<QueryStatus> FetchStatusAsync(string handle, TimeSpan? requestTimeout, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<IQueryResult> FetchResultsAsync(string handle, TimeSpan? requestTimeout, IDeserializer deserializer, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task DiscardResultsAsync(string handle, TimeSpan? requestTimeout, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task CancelQueryAsync(string requestId, TimeSpan? requestTimeout, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }
}
