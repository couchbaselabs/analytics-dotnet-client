using System.Diagnostics.CodeAnalysis;
using Couchbase.Analytics2.Internal.Utils;

namespace Couchbase.Analytics2.Internal.DI;

/// <summary>
/// Implementation of <see cref="IServiceFactory"/> which creates a transient
/// service for each request.
/// </summary>
internal sealed class TransientServiceFactory : IServiceFactory
{
    private readonly Func<IServiceProvider, object?> _factory;

    private IServiceProvider? _serviceProvider;

    /// <summary>
    /// Creates a new TransientServiceFactory which uses a lambda to create the service.
    /// </summary>
    /// <param name="factory">Lambda to invoke on each call to <see cref="CreateService"/>.</param>
    public TransientServiceFactory(Func<IServiceProvider, object?> factory)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (factory is null)
        {
            ThrowHelper.ThrowArgumentNullException(nameof(factory));
        }

        _factory = factory;
    }

    /// <summary>
    /// Creates a new TransientServiceFactory which uses a type's constructor on each call to <see cref="CreateService"/>.
    /// </summary>
    /// <param name="type">Type to create on each call to <seealso cref="CreateService"/>.</param>
    public TransientServiceFactory([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
        : this(CreateFactory(type))
    {
    }

    /// <inheritdoc />
    public void Initialize(IServiceProvider serviceProvider)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (serviceProvider is null)
        {
            ThrowHelper.ThrowArgumentNullException(nameof(serviceProvider));
        }

        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public object? CreateService(Type requestedType)
    {
        if (_serviceProvider == null)
        {
            ThrowHelper.ThrowInvalidOperationException("Not initialized.");
        }

        return _factory(_serviceProvider);
    }

    private static Func<IServiceProvider, object> CreateFactory([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implementationType)
    {
        var constructor = ConstructorSelector.SelectConstructor(implementationType);

        return Factory;

        object Factory(IServiceProvider serviceProvider)
        {
            var constructorArgs = constructor.GetParameters()
                .Select(p => serviceProvider.GetRequiredService(p.ParameterType))
                .ToArray();

            return constructor.Invoke(constructorArgs);
        }
    }
}