using System.Diagnostics.CodeAnalysis;

namespace Couchbase.Analytics2.Internal.Utils;

public class ThrowHelper
{
    [DoesNotReturn]
    public static void ThrowArgumentException(string message, string paramName) =>
        throw new ArgumentException(message, paramName);

    [DoesNotReturn]
    public static void ThrowArgumentNullException(string paramName) =>
        throw new ArgumentNullException(paramName);

    [DoesNotReturn]
    public static void ThrowArgumentOutOfRangeException() =>
        throw new ArgumentOutOfRangeException();

    [DoesNotReturn]
    public static void ThrowArgumentOutOfRangeException(string paramName) =>
        throw new ArgumentOutOfRangeException(paramName);

    [DoesNotReturn]
    public static void ThrowArgumentException(string message) =>
        throw new ArgumentException(message);

}
