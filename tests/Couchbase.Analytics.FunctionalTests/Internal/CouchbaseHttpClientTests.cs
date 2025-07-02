using System.Reflection;
using Couchbase.Analytics2.FunctionalTests.Fixtures;
using Couchbase.Analytics2.Internal.DnsUtil;
using Couchbase.Analytics2.Internal.HTTP;
using Couchbase.Analytics2.Internal.Logging;
using Xunit;
using Xunit.Abstractions;
using System.Net;
using Couchbase.Analytics2.Internal;
using Couchbase.Analytics2.Internal.DnsUtil.Strategies;
using Couchbase.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;

namespace Couchbase.Analytics2.FunctionalTests.Internal;

[Collection(TestCollection.Name)]
public class CouchbaseHttpClientTests
{
    private enum IpReplacementStrategy
    {
        None,
        Single,
        Multiple,
        All
    }

    private readonly Analytics2Fixture _fixture;
    private static ITestOutputHelper _outputHelper;
    public CouchbaseHttpClientTests(Analytics2Fixture fixture, ITestOutputHelper outputHelper)
    {
        _fixture = fixture;
        _outputHelper = outputHelper;
    }

    //This is a helper meant to allow tests to substitute some or all of the real resolved IPs with unreachable ones.
    private class TestDnsEndpointResolver : IDnsEndpointResolver
    {
        private readonly IDnsRefreshStrategy _refreshStrategy;
        private IPAddress[]? _cachedAddresses;
        private IpReplacementStrategy _ipReplacementStrategy;

        public TestDnsEndpointResolver(IDnsRefreshStrategy refreshStrategy, IpReplacementStrategy ipReplacement = IpReplacementStrategy.None)
        {
            _refreshStrategy = refreshStrategy;
            _ipReplacementStrategy = ipReplacement;
        }

        public async Task<IPAddress[]> ResolveEndpointsAsync(string hostname, CancellationToken cancellationToken)
        {
            _refreshStrategy.OnRequest();

            if (_cachedAddresses != null && !_refreshStrategy.ShouldRefreshDns()) return _cachedAddresses;
            _cachedAddresses = await System.Net.Dns.GetHostAddressesAsync(hostname, cancellationToken).ConfigureAwait(false);
            _outputHelper.WriteLine($"Resolved addresses: {string.Join(", ", _cachedAddresses.Select(a => a.ToString()))}");

            var numFalseIPs = _ipReplacementStrategy switch
            {
                IpReplacementStrategy.None => 0,
                IpReplacementStrategy.Single => 1,
                IpReplacementStrategy.Multiple => Math.Floor(_cachedAddresses.Length / 2.0), // Replace half of the addresses
                IpReplacementStrategy.All => _cachedAddresses.Length,
                _ => throw new ArgumentOutOfRangeException(nameof(_ipReplacementStrategy), "Invalid IP replacement strategy")
            };

            if (numFalseIPs > 0)
            {
                // Replace some real IPs with unreachable ones
                var falseIPs = Enumerable.Range(0, (int)numFalseIPs).Select(_ => IPAddress.Parse("192.0.2.1")).ToArray();
                var numToReplace = Math.Min(numFalseIPs, _cachedAddresses.Length);
                for (var i = 0; i < numToReplace; i++)
                {
                    _cachedAddresses[i] = falseIPs[i];
                }
                _outputHelper.WriteLine($"Replaced {numFalseIPs} real addresses with false ones. New IPs: {string.Join(", ", _cachedAddresses.Select(a => a.ToString()))}");
            }
            _refreshStrategy.OnDnsRefreshed();

            return _cachedAddresses;
        }
    }

    // Injects a "fake" IDnsEndpointResolver into the DnsEndpointConnector which can be configured to
    // replace some al or all of the resolved IPs with unreachable ones.
    [Fact]
    public async Task Test_Requests_Should_Succeed_If_Some_IPs_Are_Unreachable()
    {
        // Reflection region to replace the DnsEndpointResolver in CouchbaseHttpClientFactory
        var mockHttpClientFactoryLogger = new Mock<ILogger<CouchbaseHttpClientFactory>>();
        var mockAnalyticsLogger = new Mock<ILogger<AnalyticsService>>();
        var mockRedactor = new Mock<IRedactor>();

        var credentials = new Credential(_fixture.FixtureSettings.Username, _fixture.FixtureSettings.Password!);

        var newOptions = _fixture.ClusterOptions with { TimeoutOptions = _fixture.ClusterOptions.TimeoutOptions with {ConnectTimeout = (TimeSpan.FromSeconds(2)) }};

        var httpClientFactory = new CouchbaseHttpClientFactory(
            credentials,
            newOptions,
            mockRedactor.Object,
            mockHttpClientFactoryLogger.Object);

        var endpoint = new Uri($"{_fixture.FixtureSettings.ConnectionString}");
        var service = new AnalyticsService(_fixture.ClusterOptions, httpClientFactory, endpoint, mockAnalyticsLogger.Object, new StjJsonDeserializer());

        //Using reflection to inject another IDnsEndpointResolver into the DnsEndpointConnector
        var sharedHandlerField = typeof(CouchbaseHttpClientFactory).GetField("_sharedHandler", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var authHandler = (DelegatingHandler)sharedHandlerField.GetValue(httpClientFactory)!;
        var socketsHandler = authHandler.InnerHandler as SocketsHttpHandler;
        Assert.NotNull(socketsHandler);

        var connectCallback = socketsHandler!.ConnectCallback;
        Assert.NotNull(connectCallback);

        var target = connectCallback.Target!;
        var dnsConnector = target.GetType()
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Select(f => f.GetValue(target) as DnsEndpointConnector)
            .First();
        Assert.NotNull(dnsConnector);

        var resolverField = typeof(DnsEndpointConnector).GetField("_dnsResolver", BindingFlags.NonPublic | BindingFlags.Instance)!;

        // First, do not replace any IPs, so we can test the normal case
        var testResolver = new TestDnsEndpointResolver(new CountBasedDnsRefreshStrategy(1), ipReplacement: IpReplacementStrategy.None);
        resolverField.SetValue(dnsConnector, testResolver);
        var response = await service.SendAsync<dynamic>("SELECT hello as greeting", new QueryOptions());
        Assert.NotNull(response);

        var testResolverMissingOneIP = new TestDnsEndpointResolver(new CountBasedDnsRefreshStrategy(1), ipReplacement: IpReplacementStrategy.Single);
        resolverField.SetValue(dnsConnector, testResolverMissingOneIP);
        var response2 = await service.SendAsync<dynamic>("SELECT hello as greeting", new QueryOptions());
        Assert.NotNull(response2);

        var testResolverMissingMultipleIPs = new TestDnsEndpointResolver(new CountBasedDnsRefreshStrategy(1), ipReplacement: IpReplacementStrategy.Multiple);
        resolverField.SetValue(dnsConnector, testResolverMissingMultipleIPs);
        var response3 = await service.SendAsync<dynamic>("SELECT hello as greeting", new QueryOptions());
        Assert.NotNull(response3);

        var testResolverMissingAllIPs = new TestDnsEndpointResolver(new CountBasedDnsRefreshStrategy(1), ipReplacement: IpReplacementStrategy.All);
        resolverField.SetValue(dnsConnector, testResolverMissingAllIPs);
        await Assert.ThrowsAsync<HttpRequestException>(() => service.SendAsync<dynamic>("SELECT hello as greeting", new QueryOptions()));
    }
}