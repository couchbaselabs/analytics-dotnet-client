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
 
using System.Diagnostics.CodeAnalysis;
using Couchbase.Analytics2.Exceptions;

namespace Couchbase.Analytics2.Internal.DI;

/// <summary>
/// References a singleton of a service that isn't instantiated until required.
/// </summary>
/// <typeparam name="T"></typeparam>
internal sealed class LazyService<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T> : Lazy<T?>
    where T : notnull
{
    public LazyService(IServiceProvider serviceProvider)
        :base(serviceProvider.GetService<T>)
    {
    }

    /// <summary>
    /// Returns the services or throws if the service is not registered.
    /// </summary>
    /// <returns>The service.</returns>
    /// <exception cref="CouchbaseException">The service has not been registered.</exception>
    public T GetValueOrThrow()
    {
        var value = Value;
        if (value is null)
        {
            ThrowServiceException();
        }

        return value;
    }

    [DoesNotReturn]
    private static void ThrowServiceException() =>
        throw new AnalyticsException(
            $"Service {typeof(T).FullName} is not registered.");
}