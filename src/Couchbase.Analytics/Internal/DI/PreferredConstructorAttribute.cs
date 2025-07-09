namespace Couchbase.Analytics2.Internal.DI;

/// <summary>
/// Marks a public constructor as preferred for selection by service factories. If applied, the
/// marked constructor takes precedence over other public constructors when a factory resolves
/// the implementation type. Only a single constructor on a type may be decorated with this
/// attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
public sealed class PreferredConstructorAttribute : Attribute
{
}