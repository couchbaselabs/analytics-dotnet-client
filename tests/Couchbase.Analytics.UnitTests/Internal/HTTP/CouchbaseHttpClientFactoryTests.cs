using System;
using System.Net.Http;
using System.Threading.Tasks;
using Couchbase.AnalyticsClient;
using Couchbase.AnalyticsClient.HTTP;
using Couchbase.AnalyticsClient.Internal.HTTP;
using Couchbase.AnalyticsClient.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Couchbase.Analytics.UnitTests.Internal.HTTP;

public class CouchbaseHttpClientFactoryTests
{
    [Fact]
    public async Task CouchbaseHttpClientFactory_Dispose_CascadesToUnderlyingHandler()
    {
        // Arrange
        var options = new ClusterOptions { ConnectionString = "http://localhost:8095" };
        var factory = new CouchbaseHttpClientFactory(
            () => new Credential("Administrator", "password"), 
            options, 
            new NullLogger<CouchbaseHttpClientFactory>()
        );
        
        // Create an active HTTP client wrapping the shared AuthenticationHandler -> SocketsHttpHandler
        var httpClient = factory.Create();

        // Act
        // Executing Dispose must trigger teardowns down the chain.
        factory.Dispose();
        
        // Assert
        // A perfectly disposed downstream handler natively refuses any new HttpClient execution 
        // by immediately throwing an ObjectDisposedException preflight.
        await Assert.ThrowsAsync<ObjectDisposedException>(() => httpClient.GetAsync("http://127.0.0.1:8095"));
    }
}
