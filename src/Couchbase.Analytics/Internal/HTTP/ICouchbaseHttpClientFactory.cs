namespace Couchbase.Analytics2.Internal.HTTP;

/// <summary>
/// Creates an <see cref="HttpClient"/> which may be safely configured and disposed, but while
/// reusing inner handlers for connection pooling and HTTP keep-alives.
/// </summary>
internal interface ICouchbaseHttpClientFactory
{
    /// <summary>
    /// Creates an <see cref="HttpClient"/> which may be safely configured and disposed, but while
    /// reusing inner handlers for connection pooling and HTTP keep-alives.
    /// </summary>
    /// <returns>
    /// An <see cref="HttpClient"/> intended to be short-lived.
    /// </returns>
    /// <remarks>
    /// It is safe to dispose this after every use. It reuses the inner HttpMessageHandler.
    /// </remarks>
    HttpClient Create();

    /// <summary>
    /// Default response streaming behavior for HTTP requests. Controlled by <see cref="TuningOptions.StreamHttpResponseBodies"/>.
    /// </summary>
    HttpCompletionOption DefaultCompletionOption { get; }
}
