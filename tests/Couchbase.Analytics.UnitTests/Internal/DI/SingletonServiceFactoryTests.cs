using Couchbase.AnalyticsClient.Internal.DI;
using Moq;
using Xunit;

namespace Couchbase.Analytics.UnitTests.Internal.DI;

public class SingletonServiceFactoryTests
{
    [Fact]
    public void SingletonServiceFactory_Dispose_DisposesInnerSingleton()
    {
        // Arrange
        var mockDisposable = new Mock<IDisposable>();
        var factory = new SingletonServiceFactory(mockDisposable.Object);

        // Act
        factory.Dispose();

        // Assert
        mockDisposable.Verify(d => d.Dispose(), Times.Once);
    }
}
