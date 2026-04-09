using System;
using Couchbase.AnalyticsClient;
using Couchbase.AnalyticsClient.HTTP;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Couchbase.Analytics.UnitTests;

public class ClusterDisposalTests
{
    [Fact]
    public void Cluster_Dispose_DisposesAllRegisteredSingletons()
    {
        var mockLoggerFactory = new Mock<ILoggerFactory>();

        var cluster = Cluster.Create(
            "http://localhost:8095",
            new Credential("Administrator", "password"),
            opts => opts.WithLogging(mockLoggerFactory.Object));

        // Trigger the top-level Dispose, which must cascade down the DI structural tree
        // and aggressively purge the singleton cache.
        cluster.Dispose();

        mockLoggerFactory.Verify(x => x.Dispose(), Times.Once, "The Cluster failed to actively dispose its registered singleton resources (like the logging framework and SocketsHttpHandler) during destruction!");
    }
}
