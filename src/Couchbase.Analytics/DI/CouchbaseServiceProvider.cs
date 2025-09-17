#region License
/* ************************************************************
 *
 *    @author Couchbase <info@couchbase.com>
 *    @copyright 2025 Couchbase, Inc.
 *
 *    Licensed under the Apache License, Version 2.0 (the "License");
 *    you may not use this file except in compliance with the License.
 *    You may obtain a copy of the License at
 *
 *        http://www.apache.org/licenses/LICENSE-2.0
 *
 *    Unless required by applicable law or agreed to in writing, software
 *    distributed under the License is distributed on an "AS IS" BASIS,
 *    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *    See the License for the specific language governing permissions and
 *    limitations under the License.
 *
 * ************************************************************/
#endregion

using System.Collections.ObjectModel;

namespace Couchbase.AnalyticsClient.DI;

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