using System.Net;
using Couchbase.Analytics2.Internal.DnsUtil.Strategies;
using Xunit;
using System.Linq;
using System.Reflection;
using Xunit.Abstractions;

namespace Couchbase.Analytics2.UnitTests.Internal.DnsUtil;

public class EndpointSelectionStrategyTests
{
    private readonly ITestOutputHelper _outputHelper;
    private readonly IPAddress[] _addresses =
    [
        IPAddress.Parse("10.0.0.1"),
        IPAddress.Parse("10.0.0.2"),
        IPAddress.Parse("10.0.0.3")
    ];

    public EndpointSelectionStrategyTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }

    [Fact]
    public void RoundRobinEndpointSelector_Should_Return_Sequential_Indices()
    {
        var selector = new RoundRobinEndpointSelector();

        var actualSequence = Enumerable.Range(0, 6)
            .Select(_ => selector.SelectEndpointIndex(_addresses))
            .ToArray();

        // Expect the selection pattern to cycle the indexes of the addresses
        var expectedSequence = new[] { 0, 1, 2, 0, 1, 2 };
        Assert.Equal(expectedSequence, actualSequence);
    }

    [Fact]
    public void RoundRobinEndpointSelectorStartAtRandom_Should_Return_Sequential_Indices()
    {
        var selector = new RoundRobinEndpointSelector(startAtRandom: true);

        var selections = Enumerable.Range(0, 6)
            .Select(_ => selector.SelectEndpointIndex(_addresses))
            .ToArray();

        var firstIndex = selections[0];
        for (var i = 0; i < selections.Length; i++)
        {
            var expected = (firstIndex + i) % _addresses.Length;
            Assert.Equal(expected, selections[i]);
        }
    }

    [Fact]
    public void RandomEndpointSelector_With_PickFromUnused_Should_Select_Unique_Endpoints()
    {
        var selector = new RandomEndpointSelector(pickFromUnused: true);

        var selections = Enumerable.Range(0, 3)
            .Select(_ => selector.SelectEndpointIndex(_addresses))
            .ToArray();

        // Assert the selections contain each address exactly twice
        Assert.Equal(1, selections.Count(x => x == 0));
        Assert.Equal(1, selections.Count(x => x == 1));
        Assert.Equal(1, selections.Count(x => x == 2));
    }

    // With 3 addresses in the list, trying to select 4 times should throw an exception
    [Fact]
    public void RandomEndpointSelector_With_PickFromUnused_Should_Throw_If_No_More_Endpoints()
    {
        var selector = new RandomEndpointSelector(pickFromUnused: true);

        Assert.Throws<InvalidOperationException>( () => Enumerable.Range(0, 4).Select(_ => selector.SelectEndpointIndex(_addresses)).ToList());

    }

    /// <summary>
    /// We can't assert the exact random sequence of selections for this selector.
    /// So to reduce the chance of this test being flaky, we try to ensure that all endpoints are selected at least once after N selections.
    /// Given 3 distinct endpoints, we minimise the probability of missing 1 endpoint after N selections.
    /// i.e. f(n) = 3*(2/3)^n - 3*(1/3)^n : the probability of missing 1 IP from the selection after n selections.
    /// So the percentage of having all 3 endpoints after N selections can be written as: g(n) = (1-f(n))*100
    /// g(26) ≈ 99.99%, so we can safely use 42 selections to ensure that all endpoints are selected at least once.
    /// </summary>
    [Fact]
    public void RandomEndpointSelector_Should_Select_All_Endpoints_Over_Many_Selections()
    {
        var selector = new RandomEndpointSelector();

        var selections = Enumerable.Range(0, 42)
            .Select(_ => selector.SelectEndpointIndex(_addresses))
            .ToList();

        // Assert -> ensure that indices 0, 1, and 2 all appeared at least once
        Assert.Contains(0, selections);
        Assert.Contains(1, selections);
        Assert.Contains(2, selections);
    }
}