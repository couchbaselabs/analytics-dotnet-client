using Couchbase.AnalyticsClient.Internal.DI;
using Moq;
using Xunit;

namespace Couchbase.Analytics.UnitTests.Internal.DI;

public class TransientServiceFactoryTests
{
    [Fact]
    public void TransientServiceFactory_Dispose_DoesNotDisposeGeneratedInstances()
    {
        // Arrange
        var mockDisposable = new Mock<IDisposable>();
        var factory = new TransientServiceFactory(_ => mockDisposable.Object);

        var mockServiceProvider = new Mock<IServiceProvider>();
        factory.Initialize(mockServiceProvider.Object);

        // Act
        // We simulate the lifetime: The user asks for a Transient object, then later the Cluster shuts down entirely.
        var instance = factory.CreateService(typeof(IDisposable));

        factory.Dispose();

        // Assert
        // We explicitly confirm that the Transient factory completely ignores the instances
        // it generates, thus avoiding long-term memory leaks in the Cluster's DI container!
        Assert.NotNull(instance);
        mockDisposable.Verify(d => d.Dispose(), Times.Never);
    }
}
