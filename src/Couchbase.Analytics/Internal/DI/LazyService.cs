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