using System.Net;
using System.Reflection;
using Couchbase.AnalyticsClient.HTTP;
using Couchbase.AnalyticsClient.Internal.DnsUtil;
using Couchbase.AnalyticsClient.Internal.DnsUtil.Strategies;
using Couchbase.AnalyticsClient.Internal.HTTP;
using Couchbase.AnalyticsClient.Options;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Couchbase.AnalyticsClient.UnitTests.Internal.DnsUtil;

public class ConnectCallbackRoundRobinTests
{
    private readonly ITestOutputHelper _outputHelper;

    public ConnectCallbackRoundRobinTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }

    /// <summary>
    /// Uses reflection to extract the ConnectCallback from the SocketsHttpHandler, and verifies that it uses a RandomEndpointSelector.
    /// Then tests the selection logic to ensure it returns indices in a random manner.
    /// </summary>
    /// <exception cref="XunitException"></exception>
    [Fact]
    public void ConnectCallback_Should_Use_RandomEndpointSelector()
    {
        ICredential credential = Credential.Create("test", "test");
        var options = new ClusterOptions()
            .WithSecurityOptions(new SecurityOptions()
                .WithDisableCertificateVerification(true))
            .WithTimeoutOptions(new TimeoutOptions()
                .WithConnectTimeout(TimeSpan.FromMilliseconds(50)));

        var logger = new Mock<ILogger<CouchbaseHttpClientFactory>>().Object;
        var factory = new CouchbaseHttpClientFactory(() => credential, options, logger);

        // Extract the underlying SocketsHttpHandler and its ConnectCallback via reflection
        var sharedHandlerField = typeof(CouchbaseHttpClientFactory).GetField("_sharedHandler", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var authHandler = (DelegatingHandler)sharedHandlerField.GetValue(factory)!;
        var socketsHandler = authHandler.InnerHandler as SocketsHttpHandler;
        Assert.NotNull(socketsHandler);

        var connectCallback = socketsHandler!.ConnectCallback;
        Assert.NotNull(connectCallback);

        // The delegate's target is the closure that holds the DnsEndpointConnector instance
        var target = connectCallback.Target!;
        var dnsConnector = target.GetType()
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Select(f => f.GetValue(target) as DnsEndpointConnector)
            .First();
        Assert.NotNull(dnsConnector);

        // Retrieve the private _selectionStrategy field from DnsEndpointConnector
        var selectionField = typeof(DnsEndpointConnector)
            .GetField("_selectionStrategy", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var strategy = (IEndpointSelectionStrategy)selectionField.GetValue(dnsConnector)!;

        // Assert the strategy is of type RoundRobinEndpointSelector
        try
        {
            Assert.IsType<RandomEndpointSelector>(strategy);
        }
        catch (XunitException e)
        {
            throw new XunitException("Expected RandomEndpointSelector but got " + strategy.GetType().Name + ", did the EndpointSelectionMode change?", e);
        }

        // Further assert that the selector returns indices in round-robin order
        var addresses = new[]
        {
            IPAddress.Parse("10.0.0.1"),
            IPAddress.Parse("10.0.0.2"),
            IPAddress.Parse("10.0.0.3")
        };

        var selections = Enumerable.Range(0, 3)
            .Select(_ => strategy.SelectEndpointIndex(addresses))
            .ToArray();

        // Assert the selections contain each address exactly once
        Assert.Equal(1, selections.Count(x => x == 0));
        Assert.Equal(1, selections.Count(x => x == 1));
        Assert.Equal(1, selections.Count(x => x == 2));
    }
}

