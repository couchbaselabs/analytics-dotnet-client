using System.Diagnostics.CodeAnalysis;

namespace Couchbase.AnalyticsClient.Logging;

/// <summary>
/// An interface used for redacting specific log information.
/// </summary>
public interface IRedactor
{
    /// <summary>
    /// Redact user data like query statements, document keys, usernames.
    /// </summary>
    [return: NotNullIfNotNull(nameof(message))]
    object? UserData(object? message);

    /// <summary>
    /// Redact metadata like bucket names, dataset names, index names.
    /// </summary>
    [return: NotNullIfNotNull(nameof(message))]
    object? MetaData(object? message);

    /// <summary>
    /// Redact system data like hostnames, endpoints, URIs.
    /// </summary>
    [return: NotNullIfNotNull(nameof(message))]
    object? SystemData(object? message);
}
