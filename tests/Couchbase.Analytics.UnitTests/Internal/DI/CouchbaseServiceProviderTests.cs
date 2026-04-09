using System;
using System.Collections.Generic;
using Couchbase.AnalyticsClient.Internal.DI;
using Moq;
using Xunit;

namespace Couchbase.Analytics.UnitTests.Internal.DI;

public class CouchbaseServiceProviderTests
{
    [Fact]
    public void Dispose_IteratesAndDisposesAllDisposableFactories()
    {
        // Arrange
        var mockNonDisposableFactory = new Mock<IServiceFactory>();
        var mockDisposableFactory = new Mock<IServiceFactory>();

        // Explicitly project the second mock to support the IDisposable interface
        var disposableInterface = mockDisposableFactory.As<IDisposable>();

        var services = new Dictionary<Type, IServiceFactory>
        {
            { typeof(string), mockNonDisposableFactory.Object },
            { typeof(int), mockDisposableFactory.Object }
        };

        var provider = new CouchbaseServiceProvider(services);

        // Act
        provider.Dispose();

        // Assert
        // The normal factory should simply receive its baseline Initialize call from the constructor, and nothing else.
        mockNonDisposableFactory.Verify(f => f.Initialize(It.IsAny<IServiceProvider>()), Times.Once);

        // ...and definitively fires exactly once against any factory holding the IDisposable pattern.
        disposableInterface.Verify(d => d.Dispose(), Times.Once);
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        // Arrange
        var mockDisposableFactory = new Mock<IServiceFactory>();
        var disposableInterface = mockDisposableFactory.As<IDisposable>();

        var services = new Dictionary<Type, IServiceFactory>
        {
            { typeof(int), mockDisposableFactory.Object }
        };

        var provider = new CouchbaseServiceProvider(services);

        // Act
        provider.Dispose();
        provider.Dispose(); // Call second time
        provider.Dispose(); // Call third time

        // Assert
        // Due to the _disposed flag, multiple explicit invocations must collapse harmlessly, 
        // firing downstream teardowns EXACTLY once.
        disposableInterface.Verify(d => d.Dispose(), Times.Once);
    }

    [Fact]
    public void Dispose_ExceptionInOneFactory_DoesNotPreventDisposalOfOthers()
    {
        // Arrange
        var mockFailingFactory = new Mock<IServiceFactory>();
        var failingDisposable = mockFailingFactory.As<IDisposable>();
        failingDisposable.Setup(d => d.Dispose()).Throws(new InvalidOperationException("Boom"));

        var mockWorkingFactory = new Mock<IServiceFactory>();
        var workingDisposable = mockWorkingFactory.As<IDisposable>();

        var services = new Dictionary<Type, IServiceFactory>
        {
            { typeof(int), mockFailingFactory.Object },
            { typeof(string), mockWorkingFactory.Object }
        };

        var provider = new CouchbaseServiceProvider(services);

        // Act
        var aggregateEx = Assert.Throws<AggregateException>(() => provider.Dispose());

        // Assert
        // We guarantee the loop continued executing despite the explosion.
        Assert.Contains(aggregateEx.InnerExceptions, ex => ex is InvalidOperationException && ex.Message == "Boom");
        workingDisposable.Verify(d => d.Dispose(), Times.Once);
    }
}
