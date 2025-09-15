using Couchbase.Analytics2.Internal;
using Couchbase.Analytics2.Internal.DI;
using Xunit;
using Xunit.Abstractions;

namespace Couchbase.Analytics2.UnitTests.Internal;

public class ExecuteQueryTests
{
    private readonly ITestOutputHelper _outputHelper;

    public ExecuteQueryTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }

    // Shim used for this test
    private sealed class FakeQueryResult : IQueryResult
    {
        public IAsyncEnumerable<AnalyticsRow> Rows => this;

        public QueryMetaData MetaData { get; } = new ();

        public IReadOnlyList<Error> Errors { get; } = [];

        public System.Net.HttpStatusCode? StatusCode { get; } = null;

        public void Dispose()
        {
        }

        public IAsyncEnumerator<AnalyticsRow> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }

    // Fake service that captures the last QueryOptions it received so we can inspect them in this test
    private sealed class FakeAnalyticsService : IAnalyticsService
    {
        public Uri Uri { get; } = new Uri("http://localhost");

        public QueryOptions? LastOptions { get; private set; }

        public Task<IQueryResult> SendAsync(string statement, QueryOptions options, CancellationToken cancellationToken = default)
        {
            LastOptions = options;
            return Task.FromResult<IQueryResult>(new FakeQueryResult());
        }
    }

    // Helper method to print out QueryOptions for debugging
    private static string FormatOptions(QueryOptions o)
    {
        string FormatDict(Dictionary<string, object> d) => string.Join(", ", d.Select(kv => $"{kv.Key}={kv.Value}"));
        string FormatList(List<object> l) => string.Join(", ", l.Select(x => x?.ToString()));

        return $"AsStreaming={o.AsStreaming}; Timeout={o.Timeout}; ClientContextId={o.ClientContextId}; " +
               $"ScanConsistency={o.ScanConsistency}; ReadOnly={o.ReadOnly}; MaxRetries={o.MaxRetries}; " +
               $"Named=[{FormatDict(o.NamedParameters)}]; Positional=[{FormatList(o.PositionalParameters)}]; Raw=[{FormatDict(o.Raw)}]; " +
               $"QueryContext={(o.QueryContext is null ? "<null>" : o.QueryContext.ToString())}";
    }

    [Fact]
    public async Task ExecuteQuery_WithFunc_AppliesOptions_OnCluster()
    {
        var fakeService = new FakeAnalyticsService();

        var cluster = Cluster.Create(
            "http://localhost:18095",
            Credential.Create("user", "pass"),
            opts => opts
                .AddClusterService<IAnalyticsService>(fakeService)
        );

        QueryOptions? before = null;
        var func = (Func<QueryOptions, QueryOptions>)(o =>
        {
            // Capture the options BEFORE the func applies changes
            before = o;
            return o
                .WithAsStreaming(false)
                .WithTimeout(TimeSpan.FromSeconds(12))
                .WithClientContextId("ctx-xyz")
                .WithScanConsistency(QueryScanConsistency.RequestPlus)
                .WithReadOnly(true)
                .WithMaxRetries(5)
                .WithNamedParameters(new Dictionary<string, object> { ["k1"] = 1 })
                .WithNamedParameter("k2", 2)
                .WithPositionalParameters(["p1"])
                .WithPositionalParameter("p2")
                .WithRaw("raw1", 123);
        });

        await cluster.ExecuteQueryAsync("SELECT 1;", func);

        var applied = fakeService.LastOptions;
        Assert.NotNull(applied);

        _outputHelper.WriteLine($"Before (Cluster): {FormatOptions(before!)}");
        _outputHelper.WriteLine($"After  (Cluster): {FormatOptions(applied)}");

        Assert.False(applied!.AsStreaming);
        Assert.Equal(TimeSpan.FromSeconds(12), applied.Timeout);
        Assert.Equal("ctx-xyz", applied.ClientContextId);
        Assert.Equal(QueryScanConsistency.RequestPlus, applied.ScanConsistency);
        Assert.True(applied.ReadOnly);
        Assert.Equal((uint)5, applied.MaxRetries);
        Assert.Equal(1, applied.NamedParameters["k1"]);
        Assert.Equal(2, applied.NamedParameters["k2"]);
        Assert.Equal(["p1", "p2"], applied.PositionalParameters);
        Assert.Equal(123, applied.Raw["raw1"]);
    }

    [Fact]
    public async Task ExecuteQuery_WithFunc_AppliesOptions_OnScope_AndPreservesQueryContext()
    {
        var fakeService = new FakeAnalyticsService();

        var cluster = Cluster.Create(
            "http://localhost:18095",
            Credential.Create("user", "pass"),
            opts => opts.AddClusterService<IAnalyticsService>(fakeService)
        );

        var scope = cluster.Database("db1").Scope("sc1");

        QueryOptions? before = null;
        var func = (Func<QueryOptions, QueryOptions>)(o =>
        {
            // Capture the options before the func applies changes
            before = o;
            return o
                .WithClientContextId("scope-ctx")
                .WithAsStreaming(false);
        });

        await scope.ExecuteQueryAsync("SELECT 1;", func);

        var applied = fakeService.LastOptions;
        Assert.NotNull(applied);

        _outputHelper.WriteLine($"Before (Scope): {FormatOptions(before!)}");
        _outputHelper.WriteLine($"After  (Scope): {FormatOptions(applied!)}");

        Assert.Equal("scope-ctx", applied!.ClientContextId);
        Assert.False(applied.AsStreaming);

        // QueryContext should be set by Scope before invoking func
        Assert.NotNull(applied.QueryContext);
        Assert.Equal("db1", applied.QueryContext!.Database);
        Assert.Equal("sc1", applied.QueryContext.Scope);
        Assert.Equal("default:`db1`.`sc1`", applied.QueryContext.ToString());
    }
}