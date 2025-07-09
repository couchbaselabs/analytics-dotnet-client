using Couchbase.Analytics2.Internal.Utils;

namespace Couchbase.Analytics2.Internal.DI;

/// <summary>
/// Extensions for <seealso cref="IServiceProvider"/>.
/// </summary>
internal static class CouchbaseServiceProviderExtensions
{
    /// <summary>
    /// Gets a service, throws an exception if not registered.
    /// </summary>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
    /// <param name="type">Service being requested.</param>
    /// <returns>The service.</returns>
    public static object GetRequiredService(this IServiceProvider serviceProvider, Type type)
    {
        if (serviceProvider == null)
        {
            ThrowHelper.ThrowArgumentNullException(nameof(serviceProvider));
        }

        var service = serviceProvider.GetService(type);
        if (service == null)
        {
            ThrowHelper.ThrowInvalidOperationException($"Service {type.FullName} is not registered.");
        }

        return service;
    }

    /// <summary>
    /// Gets a service.
    /// </summary>
    /// <typeparam name="T">Service being requested.</typeparam>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
    /// <returns>The service.</returns>
    public static T? GetService<T>(this IServiceProvider serviceProvider)
    {
        if (serviceProvider == null)
        {
            ThrowHelper.ThrowArgumentNullException(nameof(serviceProvider));
        }

        return (T?) serviceProvider.GetService(typeof(T));
    }

    /// <summary>
    /// Gets a service, throws an exception if not registered.
    /// </summary>
    /// <typeparam name="T">Service being requested.</typeparam>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
    /// <returns>The service.</returns>
    public static T GetRequiredService<T>(this IServiceProvider serviceProvider) =>
        (T) serviceProvider.GetRequiredService(typeof(T));

    /// <summary>
    /// Determines if the specified service type is available from the <see cref="ICouchbaseServiceProvider"/>.
    /// </summary>
    /// <typeparam name="T">Service being tested.</typeparam>
    /// <param name="serviceProvider">The <see cref="ICouchbaseServiceProvider"/>.</param>
    /// <returns>true if the specified service is a available, false if it is not.</returns>
    public static bool IsService<T>(this ICouchbaseServiceProvider serviceProvider)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (serviceProvider is null)
        {
            ThrowHelper.ThrowArgumentNullException(nameof(serviceProvider));
        }

        return serviceProvider.IsService(typeof(T));
    }
}