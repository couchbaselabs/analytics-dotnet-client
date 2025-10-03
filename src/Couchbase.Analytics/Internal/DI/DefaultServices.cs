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

using System.Text.Json;
using Couchbase.AnalyticsClient.Internal.HTTP;
using Couchbase.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Couchbase.AnalyticsClient.Internal.DI;

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

        // Register AnalyticsService directly (it now includes retry functionality via static utilities)
        yield return (typeof(IAnalyticsService), new SingletonServiceFactory(typeof(AnalyticsService)));
    }
}