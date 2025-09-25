using System.Reflection;
using Couchbase.AnalyticsClient.FunctionalTests.Fixtures;
using Xunit;
using Xunit.Abstractions;
using System.Net;
using Couchbase.AnalyticsClient.Internal;
using Couchbase.AnalyticsClient.Internal.DI;
using Couchbase.AnalyticsClient.Internal.DnsUtil;
using Couchbase.AnalyticsClient.Internal.DnsUtil.Strategies;
using Couchbase.AnalyticsClient.Internal.HTTP;
using Couchbase.AnalyticsClient.Public.DnsUtil;
using Couchbase.AnalyticsClient.Public.Exceptions;
using Couchbase.AnalyticsClient.Public.Options;

namespace Couchbase.AnalyticsClient.FunctionalTests.Internal;

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
    private static ITestOutputHelper? _outputHelper;
    public CouchbaseHttpClientTests(Analytics2Fixture fixture, ITestOutputHelper? outputHelper)
    {
        _fixture = fixture;
        _outputHelper = outputHelper;
    }

    private static ClusterOptions SmallTimeoutOptions =>
        new ClusterOptions().WithTimeoutOptions(new TimeoutOptions().WithConnectTimeout(TimeSpan.FromSeconds(2)));

    //This is a helper meant to allow tests to substitute some or all of the real resolved IPs with unreachable ones.
    private class TestDnsEndpointResolver : IDnsEndpointResolver
    {
        private IDnsRefreshStrategy _refreshStrategy;
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
                _ => throw new ArgumentOutOfRangeException(nameof(_ipReplacementStrategy),
                    "Invalid IP replacement strategy")
            };

            if (_cachedAddresses.Length == 1 && _ipReplacementStrategy is not IpReplacementStrategy.All)
            {
                //If we have a single node cluster, we can only test IpReplacementStrategy.None and IpReplacementStrategy.All.
                // Can't replace just one IP if there's only one IP, so do none.
                numFalseIPs = 0;
            }

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
            else
            {
                _outputHelper.WriteLine("Replaced no addresses with unreachable ones.");
            }
            _refreshStrategy.OnDnsRefreshed();

            return _cachedAddresses;
        }
    }

    private void GetAnalyticsService(out IAnalyticsService service, out ICouchbaseServiceProvider serviceProvider)
    {
        serviceProvider = _fixture.ClusterOptions.BuildServiceProvider();
        Assert.NotNull(serviceProvider);
        service = serviceProvider.GetRequiredService<IAnalyticsService>();
        Assert.NotNull(service);
    }

    private void InjectFakeDnsEndpointResolver(ICouchbaseServiceProvider serviceProvider, TestDnsEndpointResolver testResolver)
    {
        // Reflection region to replace the DnsEndpointResolver in CouchbaseHttpClientFactory
        var httpClientFactory = serviceProvider.GetService<ICouchbaseHttpClientFactory>();

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
        Assert.NotNull(resolverField);

        resolverField.SetValue(dnsConnector, testResolver);
        // Ensure the connector takes the DNS path even if host is an IP
        var forceDnsProp = typeof(DnsEndpointConnector).GetProperty("ForceDnsResolution", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public);
        forceDnsProp?.SetValue(null, true);
        _outputHelper.WriteLine("Injected TestDnsEndpointResolver into DnsEndpointConnector.");
    }

    // Injects a "fake" IDnsEndpointResolver into the DnsEndpointConnector which can be configured to
    // replace some or all of the resolved IPs with unreachable ones.
    [Fact]
    public async Task Test_Requests_Should_Succeed_If_Some_IPs_Are_Unreachable()
    {
        // We need to call ResetCluster() to re-create the ClusterOptions which build the ServiceProvider,
        // because the Cluster-scoped singletons keep the first SocketsHttpHandler (and its DnsEndpointConnector).
        // Without re-setting the Singleton instance, each test reuses the same open connections,
        // so the ConnectCallback (and therefore the injected TestDnsEndpointResolver logic) doesn't run again.
        // (We could refresh the SocketsHttpHandler, but it's much cleaner to just re-create the ClusterOptions)
        _fixture.ResetCluster(SmallTimeoutOptions);
        GetAnalyticsService(out var service, out var serviceProvider);
        // First, do not replace any IPs, so we can test the normal case
        var testResolver = new TestDnsEndpointResolver(new CountBasedDnsRefreshStrategy(1), ipReplacement: IpReplacementStrategy.None);
        InjectFakeDnsEndpointResolver(serviceProvider, testResolver);
        var response = await service.SendAsync("SELECT \"hello\" as greeting", new QueryOptions());
        await foreach (var result in response.ConfigureAwait(false))
        {
            _outputHelper.WriteLine(result.ContentAs<GreetingResponse>().Greeting);
        }
        Assert.NotNull(response);
    }

    [Fact]
    public async Task Test_Requests_Should_Succeed_If_One_IP_Is_Unreachable()
    {
        _fixture.ResetCluster(SmallTimeoutOptions);
        GetAnalyticsService(out var service, out var serviceProvider);
        var testResolverMissingOneIP = new TestDnsEndpointResolver(new CountBasedDnsRefreshStrategy(1), ipReplacement: IpReplacementStrategy.Single);
        InjectFakeDnsEndpointResolver(serviceProvider, testResolverMissingOneIP);
        var response = await service.SendAsync("SELECT \"hello\" as greeting", new QueryOptions());
        await foreach (var result in response.ConfigureAwait(false))
        {
            _outputHelper.WriteLine(result.ContentAs<GreetingResponse>().Greeting);
        }
        Assert.NotNull(response);
    }

    [Fact]
    public async Task Test_Requests_Should_Succeed_If_Multiple_IPs_Are_Unreachable()
    {
        _fixture.ResetCluster(SmallTimeoutOptions);
        GetAnalyticsService(out var service, out var serviceProvider);
        var testResolverMissingMultipleIPs = new TestDnsEndpointResolver(new CountBasedDnsRefreshStrategy(1), ipReplacement: IpReplacementStrategy.Multiple);
        InjectFakeDnsEndpointResolver(serviceProvider, testResolverMissingMultipleIPs);
        var response = await service.SendAsync("SELECT \"hello\" as greeting", new QueryOptions());
        Assert.NotNull(response);

        await foreach (var result in response.ConfigureAwait(false))
        {
            Assert.Equal("hello", result.ContentAs<GreetingResponse>().Greeting);
        }
    }

    [Fact]
    public async Task Test_Requests_Should_Fail_If_All_IPs_Are_Unreachable()
    {
        _fixture.ResetCluster(SmallTimeoutOptions);
        GetAnalyticsService(out var service, out var serviceProvider);
        var testResolver = new TestDnsEndpointResolver(new CountBasedDnsRefreshStrategy(1), ipReplacement: IpReplacementStrategy.All);
        InjectFakeDnsEndpointResolver(serviceProvider, testResolver);
        await Assert.ThrowsAsync<AnalyticsException>(() => service.SendAsync("SELECT \"hello\" as greeting", new QueryOptions()));
    }
}