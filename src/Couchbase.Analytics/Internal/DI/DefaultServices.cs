using System.Text.Json;
using Couchbase.Analytics2.Internal.HTTP;
using Couchbase.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Couchbase.Analytics2.Internal.DI;

internal static class DefaultServices
{
    /// <summary>
    /// Provides the default services for a new service provider.
    /// </summary>
    /// <returns>The default services. This collection can be safely modified without side effects.</returns>
    public static IDictionary<Type, IServiceFactory> GetDefaultServices() =>
        GetDefaultServicesEnumerable().ToDictionary(p => p.Type, p => p.Factory);

    private static IEnumerable<(Type Type, IServiceFactory Factory)> GetDefaultServicesEnumerable()
    {
        yield return (typeof(ILoggerFactory), new SingletonServiceFactory(new NullLoggerFactory()));
        yield return (typeof(ILogger<>), new SingletonGenericServiceFactory(typeof(Logger<>)));

        yield return (typeof(JsonSerializerOptions), new SingletonServiceFactory(new JsonSerializerOptions()));

        yield return (typeof(IDeserializer), new SingletonServiceFactory(typeof(StjJsonDeserializer)));
        yield return (typeof(ISerializer), new SingletonServiceFactory(typeof(StjJsonSerializer)));

        yield return (typeof(ICouchbaseHttpClientFactory), new SingletonServiceFactory(typeof(CouchbaseHttpClientFactory)));
        yield return (typeof(IAnalyticsService), new SingletonServiceFactory(typeof(AnalyticsService)));
    }
}