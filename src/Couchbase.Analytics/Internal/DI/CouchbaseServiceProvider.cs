using System.Collections.ObjectModel;

namespace Couchbase.Analytics2.Internal.DI;

internal sealed class CouchbaseServiceProvider : ICouchbaseServiceProvider
{
    private readonly IReadOnlyDictionary<Type, IServiceFactory> _services;

    /// <summary>
    /// Create a new CouchbaseServiceProvider.
    /// </summary>
    /// <param name="serviceFactories">Factories keyed by type being requested.</param>
    public CouchbaseServiceProvider(IEnumerable<KeyValuePair<Type, IServiceFactory>> serviceFactories)
    {
        if (serviceFactories == null)
        {
            throw new ArgumentNullException(nameof(serviceFactories));
        }

        var serviceDictionary = serviceFactories.ToDictionary(p => p.Key, p => p.Value);
        serviceDictionary.Add(typeof(IServiceProvider), new SingletonServiceFactory(this));

        _services = new ReadOnlyDictionary<Type, IServiceFactory>(serviceDictionary);

        foreach (var service in _services)
        {
            service.Value.Initialize(this);
        }
    }

    /// <inheritdoc />
    public object? GetService(Type serviceType)
    {
        if (_services.TryGetValue(serviceType, out var factory))
        {
            return factory.CreateService( serviceType);
        }

        if (serviceType.IsGenericType && _services.TryGetValue(serviceType.GetGenericTypeDefinition(), out factory))
        {
            return factory.CreateService(serviceType);
        }

        return null;
    }

    /// <inheritdoc />
    public bool IsService(Type serviceType)
    {
        if (_services.ContainsKey(serviceType))
        {
            return true;
        }

        if (serviceType.IsGenericType && _services.ContainsKey(serviceType.GetGenericTypeDefinition()))
        {
            return true;
        }

        return false;
    }
}