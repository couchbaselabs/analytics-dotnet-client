using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace Couchbase.Analytics2.Internal.DI;

internal static class ConstructorSelector
{
    /// <summary>
    /// Returns the constructor to be used for dependency injection resolution.
    /// </summary>
    /// <param name="implementationType">Type whose constructor will be selected.</param>
    /// <returns>Selected <see cref="ConstructorInfo"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown if multiple preferred constructors are found.</exception>
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2055", Justification = "Public constructors are preserved via PublicConstructors attribute on calling site.")]
    internal static ConstructorInfo SelectConstructor([
        DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implementationType)
    {
        var constructors = implementationType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        // Prefer constructor marked with PreferredConstructorAttribute
        var preferred = constructors.Where(c =>
            c.IsDefined(typeof(PreferredConstructorAttribute), inherit: false)).ToArray();

        if (preferred.Length > 1)
        {
            throw new InvalidOperationException($"Type '{implementationType}' has more than one constructor " +
                                                "marked with [PreferredConstructorAttribute].");
        }

        if (preferred.Length == 1)
        {
            return preferred[0];
        }

        // Fallback: constructor with most parameters
        return constructors.OrderByDescending(c => c.GetParameters().Length).First();
    }
} 