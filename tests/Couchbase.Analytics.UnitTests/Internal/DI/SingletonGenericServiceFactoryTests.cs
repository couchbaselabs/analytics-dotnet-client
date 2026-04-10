using System;
using Couchbase.AnalyticsClient.Internal.DI;
using Moq;
using Xunit;

namespace Couchbase.Analytics.UnitTests.Internal.DI;

public interface IFakeGenericService<T> : IDisposable { }

public class FakeGenericService<T> : IFakeGenericService<T> 
{
    public static Action? OnDispose { get; set; }
    
    public FakeGenericService() { }
    
    public void Dispose()
    {
        OnDispose?.Invoke();
    }
}

public class SingletonGenericServiceFactoryTests
{
    [Fact]
    public void SingletonGenericServiceFactory_Dispose_DisposesAllCachedPermutations()
    {
        // Arrange
        var factory = new SingletonGenericServiceFactory(typeof(FakeGenericService<>));
        
        var mockServiceProvider = new Mock<IServiceProvider>();
        factory.Initialize(mockServiceProvider.Object);
        
        // Populate the concurrent dictionary with different permutations of the generic
        factory.CreateService(typeof(IFakeGenericService<int>));
        factory.CreateService(typeof(IFakeGenericService<string>));
        factory.CreateService(typeof(IFakeGenericService<double>));
        
        int disposeCount = 0;
        FakeGenericService<int>.OnDispose = () => disposeCount++;
        FakeGenericService<string>.OnDispose = () => disposeCount++;
        FakeGenericService<double>.OnDispose = () => disposeCount++;

        // Act
        factory.Dispose();
        
        // Assert
        // The factory should naturally iterate and call Dispose exactly 3 times (once per instantiated T)
        Assert.Equal(3, disposeCount);
        
        // Cleanup statics
        FakeGenericService<int>.OnDispose = null;
        FakeGenericService<string>.OnDispose = null;
        FakeGenericService<double>.OnDispose = null;
    }
}
