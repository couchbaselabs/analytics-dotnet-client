using System;
using System.Collections.Generic;
using Couchbase.Analytics2;
using Couchbase.Text.Json;
using Xunit;

namespace Couchbase.Analytics2.UnitTests.Internal;

public class QueryOptionTests
{
    private static QueryOptions CreateBaseline()
    {
        return new QueryOptions()
            .WithAsStreaming(false)
            .WithTimeout(TimeSpan.FromSeconds(7))
            .WithClientContextId("ctx-1")
            .WithScanConsistency(QueryScanConsistency.NotBounded)
            .WithReadOnly(true)
            .WithMaxRetries(3)
            .WithNamedParameters(new Dictionary<string, object> { ["a"] = 1 })
            .WithNamedParameter("b", 2)
            .WithPositionalParameters(["x"])
            .WithPositionalParameter("y")
            .WithRawParameters(new Dictionary<string, object> { ["foo"] = "bar" })
            .WithRaw("baz", 9)
            .WithDeserializer(new StjJsonDeserializer());
    }

    [Fact]
    public void WithAsStreaming_ReturnsNew_And_DoesNotMutatePrevious()
    {
        var options = CreateBaseline();
        var updated = options.WithAsStreaming(true);

        Assert.NotSame(options, updated);
        Assert.False(options.AsStreaming);
        Assert.True(updated.AsStreaming);
    }

    [Fact]
    public void WithTimeout_ReturnsNew_And_DoesNotMutatePrevious()
    {
        var options = CreateBaseline();
        var newTimeout = TimeSpan.FromSeconds(30);
        var updated = options.WithTimeout(newTimeout);

        Assert.NotSame(options, updated);
        Assert.Equal(TimeSpan.FromSeconds(7), options.Timeout);
        Assert.Equal(newTimeout, updated.Timeout);
    }

    [Fact]
    public void WithClientContextId_ReturnsNew_And_DoesNotMutatePrevious()
    {
        var options = CreateBaseline();
        var updated = options.WithClientContextId("ctx-2");

        Assert.NotSame(options, updated);
        Assert.Equal("ctx-1", options.ClientContextId);
        Assert.Equal("ctx-2", updated.ClientContextId);
    }

    [Fact]
    public void WithScanConsistency_ReturnsNew_And_DoesNotMutatePrevious()
    {
        var options = CreateBaseline();
        var updated = options.WithScanConsistency(QueryScanConsistency.RequestPlus);

        Assert.NotSame(options, updated);
        Assert.Equal(QueryScanConsistency.NotBounded, options.ScanConsistency);
        Assert.Equal(QueryScanConsistency.RequestPlus, updated.ScanConsistency);
    }

    [Fact]
    public void WithReadOnly_ReturnsNew_And_DoesNotMutatePrevious()
    {
        var options = CreateBaseline();
        var updated = options.WithReadOnly(false);

        Assert.NotSame(options, updated);
        Assert.True(options.ReadOnly);
        Assert.False(updated.ReadOnly);
    }

    [Fact]
    public void WithMaxRetries_ReturnsNew_And_DoesNotMutatePrevious()
    {
        var options = CreateBaseline();
        var updated = options.WithMaxRetries(10);

        Assert.NotSame(options, updated);
        Assert.Equal((uint)3, options.MaxRetries);
        Assert.Equal((uint)10, updated.MaxRetries);
    }

    [Fact]
    public void WithNamedParameters_Replaces_Set_ReturnsNew_And_DoesNotMutatePrevious()
    {
        var options = CreateBaseline();
        var newSet = new Dictionary<string, object> { ["c"] = 3 };
        var updated = options.WithNamedParameters(newSet);

        Assert.NotSame(options, updated);
        Assert.Contains("a", options.NamedParameters);
        Assert.Contains("b", options.NamedParameters);
        Assert.DoesNotContain("c", options.NamedParameters);

        Assert.Single(updated.NamedParameters);
        Assert.Equal(3, updated.NamedParameters["c"]);
    }

    [Fact]
    public void WithNamedParameter_Updates_Copy_ReturnsNew_And_DoesNotMutatePrevious()
    {
        var options = CreateBaseline();
        var updated = options.WithNamedParameter("d", 4);

        Assert.NotSame(options, updated);
        Assert.DoesNotContain("d", options.NamedParameters);
        Assert.Equal(4, updated.NamedParameters["d"]);
        Assert.Equal(2, updated.NamedParameters["b"]);
    }

    [Fact]
    public void WithPositionalParameters_Replaces_Set_ReturnsNew_And_DoesNotMutatePrevious()
    {
        var options = CreateBaseline();
        var updated = options.WithPositionalParameters([1, 2, 3]);

        Assert.NotSame(options, updated);
        Assert.Equal(["x", "y"], options.PositionalParameters);
        Assert.Equal([1, 2, 3], updated.PositionalParameters);
    }

    [Fact]
    public void WithPositionalParameter_Appends_Copy_ReturnsNew_And_DoesNotMutatePrevious()
    {
        var options = CreateBaseline();
        var updated = options.WithPositionalParameter("z");

        Assert.NotSame(options, updated);
        Assert.Equal(["x", "y"], options.PositionalParameters);
        Assert.Equal(["x", "y", "z"], updated.PositionalParameters);
    }

    [Fact]
    public void WithRawParameters_Replaces_Set_ReturnsNew_And_DoesNotMutatePrevious()
    {
        var options = CreateBaseline();
        var newSet = new Dictionary<string, object> { ["alpha"] = 1 };
        var updated = options.WithRawParameters(newSet);

        Assert.NotSame(options, updated);
        Assert.Contains("foo", options.Raw);
        Assert.DoesNotContain("alpha", options.Raw);
        Assert.Single(updated.Raw);
        Assert.Equal(1, updated.Raw["alpha"]);
    }

    [Fact]
    public void WithRaw_AddsOrUpdates_ReturnsNew_And_DoesNotMutatePrevious()
    {
        var options = CreateBaseline();
        var updated = options.WithRaw("hello", "world");

        Assert.NotSame(options, updated);
        Assert.DoesNotContain("hello", options.Raw);
        Assert.Equal("world", updated.Raw["hello"]);
        Assert.Equal("bar", updated.Raw["foo"]);
    }
}