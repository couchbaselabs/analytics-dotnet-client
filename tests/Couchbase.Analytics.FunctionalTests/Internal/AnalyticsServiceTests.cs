using System.Diagnostics;
using System.Net;
using System.Xml.Xsl;
using Couchbase.Analytics2.FunctionalTests.Fixtures;
using Couchbase.Analytics2.Internal;
using Xunit;
using Couchbase.Analytics2.Internal.HTTP;
using Couchbase.Analytics2.Internal.Logging;
using Couchbase.Text.Json;
using DnsClient;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit.Abstractions;

namespace Couchbase.Analytics2.FunctionalTests.Internal;

[Collection(TestCollection.Name)]
public class AnalyticsServiceTests
{
    private readonly Analytics2Fixture _analytics2Fixture;
    private readonly ITestOutputHelper _outputHelper;

    public AnalyticsServiceTests(Analytics2Fixture analytics2Fixture, ITestOutputHelper outputHelper)
    {
        _analytics2Fixture = analytics2Fixture;
        _outputHelper = outputHelper;
    }

    [Fact]
    public async Task TestGetAnalyticsAsync()
    {
        var mockHttpClientFactory = new Mock<ILogger<CouchbaseHttpClientFactory>>();
        var mockAnalyticsLogger = new Mock<ILogger<AnalyticsService>>();
        var mockRedactor = new Mock<IRedactor>();

        var credentials = new Credential(_analytics2Fixture.FixtureSettings.Username, _analytics2Fixture.FixtureSettings.Password!);

        var httpClientFactory = new CouchbaseHttpClientFactory(
            credentials,
            _analytics2Fixture.ClusterOptions,
            mockRedactor.Object,
            mockHttpClientFactory.Object);

        var endpoint = new Uri($"https://{_analytics2Fixture.FixtureSettings.ConnectionString}");
        var service = new AnalyticsService(_analytics2Fixture.ClusterOptions, httpClientFactory, endpoint, mockAnalyticsLogger.Object, new StjJsonDeserializer());

        var response = await service.SendAsync<dynamic>("SELECT 1;", new QueryOptions());
        Assert.NotNull(response);
    }

    [Fact]
    public async Task Test_DNSLookupAsync()
    {
        var lookupClient = new LookupClient();
        var hostname = _analytics2Fixture.FixtureSettings.ConnectionString!.Split(':')[0]; // Extract hostname from connection string
        var results = await lookupClient.QueryAsync(hostname, QueryType.A);
        Assert.NotNull(results);
    }
}