using System.Diagnostics;
using System.Net;
using System.Xml.Xsl;
using Couchbase.Analytics2.Internal;
using Xunit;
using Couchbase.Analytics2.Internal.HTTP;
using Couchbase.Analytics2.Internal.Logging;
using Microsoft.Extensions.Logging;
using Moq;

namespace Couchbase.Analytics2.FunctionalTests.Internal;

public class AnalyticsServiceTests
{
    [Fact]
    public async Task TestGetAnalyticsAsync()
    {
        var mockHttpClientFactory = new Mock<ILogger<CouchbaseHttpClientFactory>>();
        var mockAnalyticsLogger = new Mock<ILogger<AnalyticsService>>();
        var mockRedactor = new Mock<IRedactor>();
        var credentials = new Credential("Administrator", "password");
        var options = new ClusterOptions();
        options.SecurityOptions.TrustOnlyPemString(cert);

        var httpClientFactory = new CouchbaseHttpClientFactory(
            credentials,
            options.SecurityOptions,
            mockRedactor.Object,
            mockHttpClientFactory.Object);

        var addresses = System.Net.Dns.GetHostAddresses("5e07bed7-20250516.cb-sdk.bemdas.com");
        var ipEndpoint = new IPEndPoint(addresses.First(), 8095);
        var service = new AnalyticsService(options, httpClientFactory, ipEndpoint, mockAnalyticsLogger.Object);

        var response = await service.SendAsync<dynamic>("SELECT 1;", new QueryOptions());
        Assert.NotNull(response);
    }

    private const string cert =
        "-----BEGIN CERTIFICATE-----\n"+
        "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\n"+
        "-----END CERTIFICATE-----\n";

}
