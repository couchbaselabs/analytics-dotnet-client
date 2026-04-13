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

using System.Net;
using System.Text;
using System.Text.Json;
using Couchbase.AnalyticsClient.Async;
using Couchbase.AnalyticsClient.Exceptions;
using Couchbase.AnalyticsClient.HTTP;
using Couchbase.AnalyticsClient.Internal.HTTP;
using Couchbase.AnalyticsClient.Internal.Results;
using Couchbase.AnalyticsClient.Internal.Retry;
using Couchbase.AnalyticsClient.Logging;
using Couchbase.AnalyticsClient.Options;
using Couchbase.AnalyticsClient.Query;
using Couchbase.AnalyticsClient.Results;
using Couchbase.Core.Json;
using Couchbase.Core.Utils;
using Microsoft.Extensions.Logging;

namespace Couchbase.AnalyticsClient.Internal;

internal sealed partial class AnalyticsService : HttpServiceBase, IAnalyticsService
{
    private readonly ClusterOptions _clusterOptions;
    private readonly ILogger<AnalyticsService> _logger;
    private readonly TypedRedactor _redactor;
    private readonly Uri _baseUri;

    public AnalyticsService(ClusterOptions clusterOptions, ICouchbaseHttpClientFactory httpClientFactory,
        ILogger<AnalyticsService> logger, TypedRedactor redactor) : base(httpClientFactory)
    {
        _clusterOptions = clusterOptions;
        _logger = logger;
        _redactor = redactor;
        HttpClientFactory = httpClientFactory;
        Uri = clusterOptions.ConnectionStringValue!.GetAnalyticsServiceUri();
        _baseUri = clusterOptions.ConnectionStringValue!.GetBaseServiceUri();
    }

    public Uri Uri { get; }

    // ────────────────────────────────────────────────────────────
    // Synchronous query API (existing)
    // ────────────────────────────────────────────────────────────

    public async Task<IQueryResult> SendAsync(string statement, QueryOptions options, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithRetryAsync(statement, options, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Core query execution logic - the golden path for sending analytics requests.
    /// </summary>
    private async Task<IQueryResult> ExecuteQueryAsync(StringContent content, HttpClient httpClient, bool asStreaming, IDeserializer deserializer, ErrorContext errorContext, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, Uri)
        {
            Content = content
        };

        try
        {
            var response = await httpClient.SendAsync(request,
                    asStreaming ? HttpCompletionOption.ResponseHeadersRead : HttpCompletionOption.ResponseContentRead,
                    cancellationToken)
                .ConfigureAwait(false);

            var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

            AnalyticsResultBase result = asStreaming
                ? new StreamingAnalyticsResult(stream, deserializer, httpClient)
                : new BlockingAnalyticsResult(stream, deserializer, httpClient);

            result.StatusCode = response.StatusCode;
            errorContext.StatusCode = response.StatusCode;

            await result.InitializeAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw AnalyticsErrorMapper.MapHttpErrorCode(result, errorContext);
            }

            return result;
        }
        catch (TaskCanceledException taskCanceledEx)
        {
            throw new AnalyticsTimeoutException("The analytics request was canceled via its cancellation token.", taskCanceledEx, errorContext);
        }
    }

    /// <summary>
    /// Retry wrapper around the core query execution logic.
    /// </summary>
    private async Task<IQueryResult> ExecuteWithRetryAsync(string statement, QueryOptions options, CancellationToken cancellationToken = default)
    {
        var stopwatch = LightweightStopwatch.StartNew();
        // The Timeout of QueryOptions is nullable. If it wasn't set by the user, we must set it to the default from ClusterOptions.
        var timeout = options.Timeout ?? _clusterOptions.TimeoutOptions.QueryTimeout;
        options = options with { Timeout = timeout };

        // The QueryOptions' Deserializer should override the ClusterOptions'.
        var deserializer = options.Deserializer ?? _clusterOptions.Deserializer;

        var errorContext = new ErrorContext(options.ClientContextId, stopwatch, timeout);
        Exception? lastException = null;

        var body = options.GetFormValuesAsJson(statement);
        using var content = new StringContent(body, Encoding.UTF8, MediaType.Json);

        // This timeout is per-attempt i.e. per http.SendAsync().
        var httpClient = CreateHttpClient(timeout);

        var maxRetries = options.MaxRetries ?? _clusterOptions.MaxRetries;

        var attempt = -1;

        while (attempt < maxRetries)
        {
            attempt++;
            errorContext.RetryAttempts = attempt;

            // We observe the overall timeout across all retries.
            if (stopwatch.Elapsed > timeout)
            {
                ThrowGlobalTimeout(lastException, stopwatch.Elapsed, errorContext);
            }
            try
            {
                LogQueryAttemptStarting(_logger, attempt + 1, options.ClientContextId, options.QueryContext?.ToString() ?? "<cluster>", _redactor.UserData(statement), stopwatch.Elapsed.TotalMilliseconds);

                var result = await ExecuteQueryAsync(content, httpClient, options.AsStreaming, deserializer, errorContext, cancellationToken).ConfigureAwait(false);

                // Always read errors from the result
                if (result.Errors is { Count: > 0 })
                {
                    // Per RFC: retry if ALL errors are retriable
                    if (AnalyticsErrorMapper.AreErrorsRetriable(result.Errors))
                    {
                        LogRetriableServerErrors(_logger, options.ClientContextId);

                        lastException = AnalyticsErrorMapper.MapServiceErrors(result.Errors, errorContext);

                        await RetryUtils.BackoffAsync(attempt, cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    // Non-retriable server errors - throw
                    throw AnalyticsErrorMapper.MapServiceErrors(result.Errors, errorContext);
                }

                return result;
            }
            catch (HttpRequestException httpRequestException)
            {
                LogQueryAttemptFailed(_logger, httpRequestException, attempt + 1, options.ClientContextId, options.QueryContext?.ToString() ?? "<cluster>", _redactor.UserData(statement),
                    httpRequestException.Message, stopwatch.Elapsed.TotalMilliseconds);

                // "No successful connection(s)" is retryable
                if (httpRequestException.InnerException is AggregateException aggregateEx)
                {
                    lastException = new AnalyticsException("No connections could be established to any of the endpoints.", aggregateEx, errorContext);
                }

                if (!AnalyticsErrorMapper.IsRetriableHttpException(httpRequestException))
                {
                    LogNonRetriableHttpException(_logger);
                    throw;
                }

                await RetryUtils.BackoffAsync(attempt, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException operationCanceledException)
            {
                lastException = operationCanceledException;

                // Check if we exceeded the query timeout
                if (stopwatch.Elapsed > timeout)
                {
                    ThrowGlobalTimeout(lastException, stopwatch.Elapsed, errorContext);
                }

                await RetryUtils.BackoffAsync(attempt, cancellationToken).ConfigureAwait(false);
            }
        }

        throw lastException ?? ThrowTooManyRetries(errorContext);
    }

    // ────────────────────────────────────────────────────────────
    // Async server request API (new)
    // ────────────────────────────────────────────────────────────

    public async Task<QueryHandle> StartQueryAsync(string statement, StartQueryOptions options, CancellationToken cancellationToken = default)
    {
        var stopwatch = LightweightStopwatch.StartNew();
        var queryTimeout = options.QueryTimeout ?? _clusterOptions.TimeoutOptions.QueryTimeout;
        var requestTimeout = _clusterOptions.TimeoutOptions.DispatchTimeout;

        var errorContext = new ErrorContext(options.ClientContextId, stopwatch, queryTimeout);
        Exception? lastException = null;

        var body = options.GetFormValuesAsJson(statement);
        using var content = new StringContent(body, Encoding.UTF8, MediaType.Json);
        var httpClient = CreateHttpClient(requestTimeout);

        var maxRetries = options.MaxRetries ?? _clusterOptions.MaxRetries;
        var attempt = -1;

        while (attempt < maxRetries)
        {
            attempt++;
            errorContext.RetryAttempts = attempt;

            if (stopwatch.Elapsed > queryTimeout)
            {
                ThrowGlobalTimeout(lastException, stopwatch.Elapsed, errorContext);
            }

            try
            {
                LogAsyncStartQueryAttempt(_logger, attempt + 1, _redactor.SystemData(Uri), options.ClientContextId, options.QueryContext?.ToString() ?? "<cluster>", _redactor.UserData(statement), stopwatch.Elapsed.TotalMilliseconds);

                var request = new HttpRequestMessage(HttpMethod.Post, Uri) { Content = content };
                var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken)
                    .ConfigureAwait(false);

                errorContext.StatusCode = response.StatusCode;

                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                var json = JsonDocument.Parse(responseBody);
                var root = json.RootElement;

                if (!response.IsSuccessStatusCode)
                {
                    // Try to parse errors from the response
                    if (root.TryGetProperty("errors", out var errorsElement))
                    {
                        var errors = JsonSerializer.Deserialize<QueryError[]>(errorsElement.GetRawText())
                                     ?? Array.Empty<QueryError>();

                        if (AnalyticsErrorMapper.AreErrorsRetriable(errors))
                        {
                            lastException = AnalyticsErrorMapper.MapServiceErrors(errors, errorContext);
                            await RetryUtils.BackoffAsync(attempt, cancellationToken).ConfigureAwait(false);
                            continue;
                        }

                        throw AnalyticsErrorMapper.MapServiceErrors(errors, errorContext);
                    }

                    // 503 is retriable
                    if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
                    {
                        lastException = new AnalyticsException("Service temporarily unavailable.", errorContext);
                        await RetryUtils.BackoffAsync(attempt, cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    throw new AnalyticsException($"HTTP {(int)response.StatusCode} {response.StatusCode}", errorContext);
                }

                // Parse the successful response
                var requestId = root.TryGetProperty("requestID", out var reqIdProp) ? reqIdProp.GetString() : null;
                var handlePath = root.TryGetProperty("handle", out var handleProp) ? handleProp.GetString() : null;

                if (string.IsNullOrWhiteSpace(requestId) || string.IsNullOrWhiteSpace(handlePath))
                {
                    throw new AnalyticsException("Server response is missing required 'requestID' or 'handle' fields.", errorContext);
                }

                LogAsyncStartQuerySucceeded(_logger, options.ClientContextId, _redactor.SystemData(handlePath), _redactor.SystemData(requestId), (int)response.StatusCode);
                return new QueryHandle(handlePath, requestId, root.GetRawText(), this);
            }
            catch (HttpRequestException httpRequestException)
            {
                LogAsyncStartQueryFailed(_logger, httpRequestException, attempt + 1, options.ClientContextId, options.QueryContext?.ToString() ?? "<cluster>", _redactor.UserData(statement), httpRequestException.Message);

                if (httpRequestException.InnerException is AggregateException aggregateEx)
                {
                    lastException = new AnalyticsException("No connections could be established to any of the endpoints.", aggregateEx, errorContext);
                }

                if (!AnalyticsErrorMapper.IsRetriableHttpException(httpRequestException))
                {
                    throw;
                }

                await RetryUtils.BackoffAsync(attempt, cancellationToken).ConfigureAwait(false);
            }
            catch (TaskCanceledException taskCanceledEx)
            {
                throw new AnalyticsTimeoutException("The async StartQuery request was canceled.", taskCanceledEx, errorContext);
            }
            catch (OperationCanceledException)
            {
                if (stopwatch.Elapsed > queryTimeout)
                {
                    ThrowGlobalTimeout(lastException, stopwatch.Elapsed, errorContext);
                }

                await RetryUtils.BackoffAsync(attempt, cancellationToken).ConfigureAwait(false);
            }
        }

        throw lastException ?? ThrowTooManyRetries(errorContext);
    }

    public async Task<QueryResultHandle?> FetchResultHandleAsync(QueryHandle handle, FetchResultHandleOptions options, CancellationToken cancellationToken = default)
    {
        var timeout = _clusterOptions.TimeoutOptions.DispatchTimeout;
        var httpClient = CreateHttpClient(timeout);

        var statusUri = Uri.TryCreate(handle.Handle, UriKind.Absolute, out var absUri) && (absUri.Scheme == Uri.UriSchemeHttp || absUri.Scheme == Uri.UriSchemeHttps)
            ? absUri
            : new Uri(_baseUri, handle.Handle);
            
        var request = new HttpRequestMessage(HttpMethod.Get, statusUri);

        LogFetchResultHandleRequest(_logger, _redactor.SystemData(statusUri), _redactor.SystemData(handle.Handle));

        try
        {
            var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken)
                .ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new QueryNotFoundException("Query has been discarded or canceled (404 Not Found).");
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var json = JsonDocument.Parse(responseBody);
            var root = json.RootElement;

            var status = root.TryGetProperty("status", out var statusProp) ? statusProp.GetString() : null;

            if (string.IsNullOrWhiteSpace(status))
            {
                throw new AnalyticsException("Server response is missing required 'status' field.");
            }

            if (!response.IsSuccessStatusCode)
            {
                LogFetchResultHandleUnexpectedHttp(_logger, _redactor.SystemData(handle.Handle), (int)response.StatusCode);
            }

            IReadOnlyList<QueryError>? errors = null;
            if (root.TryGetProperty("errors", out var errorsElement))
            {
                errors = JsonSerializer.Deserialize<QueryError[]>(errorsElement.GetRawText()) ?? Array.Empty<QueryError>();
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorContext = new ErrorContext(string.Empty, LightweightStopwatch.StartNew(), timeout);
                errorContext.StatusCode = response.StatusCode;
                if (errors is { Count: > 0 })
                {
                    throw AnalyticsErrorMapper.MapServiceErrors(errors, errorContext);
                }
                throw new AnalyticsException($"Query status fetch failed with HTTP {(int)response.StatusCode} and status: {status}", errorContext);
            }

            LogFetchResultHandleResponse(_logger, _redactor.SystemData(handle.Handle), status, (int)response.StatusCode);

            if (string.Equals(status, "running", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (!string.Equals(status, "success", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(status, "stopped", StringComparison.OrdinalIgnoreCase) || 
                    string.Equals(status, "aborted", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(status, "closed", StringComparison.OrdinalIgnoreCase))
                {
                    throw new QueryNotFoundException($"Query has been discarded or canceled (status: {status}).");
                }
                
                if (string.Equals(status, "timeout", StringComparison.OrdinalIgnoreCase))
                {
                    throw new AnalyticsTimeoutException("The query evaluation timed out on the server.");
                }
                
                if (string.Equals(status, "fatal", StringComparison.OrdinalIgnoreCase) || 
                    string.Equals(status, "failed", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(status, "errors", StringComparison.OrdinalIgnoreCase))
                {
                    if (errors is { Count: > 0 })
                    {
                        var errorContext = new ErrorContext(string.Empty, LightweightStopwatch.StartNew(), timeout);
                        errorContext.StatusCode = response.StatusCode;
                        throw AnalyticsErrorMapper.MapServiceErrors(errors, errorContext);
                    }
                    throw new AnalyticsException($"Query execution failed on the server (status: {status}).");
                }
                
                throw new AnalyticsException($"Query status fetch failed with unrecognized status: {status}");
            }

            var resultHandle = root.TryGetProperty("handle", out var handleProp) ? handleProp.GetString() : null;
            if (string.IsNullOrWhiteSpace(resultHandle))
            {
                throw new InvalidOperationException("Query status indicates success but no result handle was provided by the server.");
            }

            return new QueryResultHandle(resultHandle, handle.RequestId, root.GetRawText(), this);
        }
        catch (TaskCanceledException taskCanceledEx)
        {
            throw new AnalyticsTimeoutException("The FetchResultHandle request was canceled.", taskCanceledEx);
        }
    }

    public async Task<IQueryResult> FetchResultsAsync(string requestId, string handlePath, FetchResultsOptions options, CancellationToken cancellationToken = default)
    {
        var timeout = _clusterOptions.TimeoutOptions.DispatchTimeout;
        var httpClient = CreateHttpClient(timeout);
        var deserializer = options.Deserializer ?? _clusterOptions.Deserializer;

        var resultUri = Uri.TryCreate(handlePath, UriKind.Absolute, out var absUri) && (absUri.Scheme == Uri.UriSchemeHttp || absUri.Scheme == Uri.UriSchemeHttps)
            ? absUri
            : new Uri(_baseUri, handlePath);
            
        var request = new HttpRequestMessage(HttpMethod.Get, resultUri);

        LogFetchResultsRequest(_logger, _redactor.SystemData(resultUri), _redactor.SystemData(handlePath));

        try
        {
            var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            LogFetchResultsResponse(_logger, _redactor.SystemData(handlePath), (int)response.StatusCode);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new QueryNotFoundException("Query results have been discarded or canceled (404 Not Found).");
            }

            var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

            // Reuse the existing StreamingAnalyticsResult - the response format is identical to sync queries
            var result = new StreamingAnalyticsResult(stream, deserializer, httpClient);
            result.StatusCode = response.StatusCode;

            await result.InitializeAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorContext = new ErrorContext(string.Empty, LightweightStopwatch.StartNew(), timeout);
                errorContext.StatusCode = response.StatusCode;
                throw AnalyticsErrorMapper.MapHttpErrorCode(result, errorContext);
            }

            return result;
        }
        catch (TaskCanceledException taskCanceledEx)
        {
            throw new AnalyticsTimeoutException("The FetchResults request was canceled.", taskCanceledEx);
        }
    }

    public async Task DiscardResultsAsync(string requestId, string handlePath, DiscardResultsOptions options, CancellationToken cancellationToken = default)
    {
        var timeout = _clusterOptions.TimeoutOptions.DispatchTimeout;
        var httpClient = CreateHttpClient(timeout);

        var resultUri = Uri.TryCreate(handlePath, UriKind.Absolute, out var absUri) && (absUri.Scheme == Uri.UriSchemeHttp || absUri.Scheme == Uri.UriSchemeHttps)
            ? absUri
            : new Uri(_baseUri, handlePath);
            
        var request = new HttpRequestMessage(HttpMethod.Delete, resultUri);

        LogDiscardResultsRequest(_logger, _redactor.SystemData(resultUri), _redactor.SystemData(handlePath));

        try
        {
            var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken)
                .ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                // Per spec: 404 means already discarded or canceled — not an error
                LogDiscardResults404(_logger, _redactor.SystemData(handlePath));
                return;
            }

            LogDiscardResultsResponse(_logger, _redactor.SystemData(handlePath), (int)response.StatusCode);

            if (response.StatusCode != HttpStatusCode.Accepted)
            {
                throw new AnalyticsException($"Unexpected response from DiscardResults: HTTP {(int)response.StatusCode} {response.StatusCode}");
            }
        }
        catch (TaskCanceledException taskCanceledEx)
        {
            throw new AnalyticsTimeoutException("The DiscardResults request was canceled.", taskCanceledEx);
        }
    }

    public async Task CancelQueryAsync(string requestId, CancelOptions options, CancellationToken cancellationToken = default)
    {
        var timeout = _clusterOptions.TimeoutOptions.DispatchTimeout;
        var httpClient = CreateHttpClient(timeout);

        var cancelUri = new Uri(_baseUri, "api/v1/active_requests");
        var request = new HttpRequestMessage(HttpMethod.Delete, cancelUri)
        {
            // Per spec: content-type must be application/x-www-form-urlencoded
            Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("request_id", requestId)
            })
        };

        LogCancelQueryRequest(_logger, _redactor.SystemData(cancelUri), _redactor.SystemData(requestId));

        try
        {
            var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken)
                .ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                // Per spec: 404 means already discarded or canceled — not an error
                LogCancelQuery404(_logger, _redactor.SystemData(requestId));
                return;
            }

            LogCancelQueryResponse(_logger, _redactor.SystemData(requestId), (int)response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                throw new AnalyticsException($"Unexpected response from CancelQuery: HTTP {(int)response.StatusCode} {response.StatusCode}");
            }
        }
        catch (TaskCanceledException taskCanceledEx)
        {
            throw new AnalyticsTimeoutException("The CancelQuery request was canceled.", taskCanceledEx);
        }
    }

    // ────────────────────────────────────────────────────────────
    // Shared helpers
    // ────────────────────────────────────────────────────────────

    private static void ThrowGlobalTimeout(Exception? lastException, TimeSpan elapsed, ErrorContext? errorContext = null)
    {
        var timeoutException = new AnalyticsTimeoutException(
            $"Analytics query timed-out after {elapsed.TotalSeconds:F2} seconds.", lastException, errorContext);
        throw timeoutException;
    }

    private static Exception ThrowTooManyRetries(ErrorContext errorContext)
    {
        throw new AnalyticsException("Exceeded maximum number of retries.", errorContext: errorContext);
    }

    #region Logging

    [LoggerMessage(1, LogLevel.Debug, "Analytics query attempt {Attempt} starting for {ClientContextId} on context [{QueryContext}]: {Statement} (elapsed: {Elapsed}ms)")]
    private static partial void LogQueryAttemptStarting(ILogger logger, int attempt, string? clientContextId, string queryContext, Redacted<string> statement, double elapsed);

    [LoggerMessage(2, LogLevel.Debug, "Received retriable server errors for ClientContextId {ClientContextId}, retrying...")]
    private static partial void LogRetriableServerErrors(ILogger logger, string? clientContextId);

    [LoggerMessage(3, LogLevel.Debug, "HttpRequestException is not retriable, failing immediately")]
    private static partial void LogNonRetriableHttpException(ILogger logger);

    [LoggerMessage(4, LogLevel.Debug, "Analytics query attempt {Attempt} for ClientContextId {ClientContextId} on context [{QueryContext}]: {Statement} failed: {Error} (elapsed: {Elapsed}ms)")]
    private static partial void LogQueryAttemptFailed(ILogger logger, Exception ex, int attempt, string? clientContextId, string queryContext, Redacted<string> statement, string error, double elapsed);

    [LoggerMessage(5, LogLevel.Debug, "Async StartQuery attempt {Attempt} sending POST to {Uri} for {ClientContextId} on context [{QueryContext}]: {Statement} (elapsed: {Elapsed}ms)")]
    private static partial void LogAsyncStartQueryAttempt(ILogger logger, int attempt, Redacted<Uri> uri, string? clientContextId, string queryContext, Redacted<string> statement, double elapsed);

    [LoggerMessage(6, LogLevel.Debug, "Async StartQuery attempt {Attempt} for {ClientContextId} on context [{QueryContext}]: {Statement} failed: {Error}")]
    private static partial void LogAsyncStartQueryFailed(ILogger logger, Exception ex, int attempt, string? clientContextId, string queryContext, Redacted<string> statement, string error);

    [LoggerMessage(7, LogLevel.Debug, "DiscardResults returned 404 for handle {Handle} — already discarded or canceled.")]
    private static partial void LogDiscardResults404(ILogger logger, Redacted<string> handle);

    [LoggerMessage(8, LogLevel.Debug, "CancelQuery returned 404 for requestId {RequestId} — already discarded or canceled.")]
    private static partial void LogCancelQuery404(ILogger logger, Redacted<string> requestId);

    // ── Async API: request-sent / response-received pairs ──

    [LoggerMessage(9, LogLevel.Debug, "Async StartQuery succeeded for {ClientContextId}. Handle={Handle}, RequestId={RequestId} (HTTP {StatusCode})")]
    private static partial void LogAsyncStartQuerySucceeded(ILogger logger, string? clientContextId, Redacted<string> handle, Redacted<string> requestId, int statusCode);

    [LoggerMessage(10, LogLevel.Debug, "FetchResultHandle sending GET to {Uri} for handle {Handle}")]
    private static partial void LogFetchResultHandleRequest(ILogger logger, Redacted<Uri> uri, Redacted<string> handle);

    [LoggerMessage(11, LogLevel.Debug, "FetchResultHandle for handle {Handle} returned status={Status} (HTTP {StatusCode})")]
    private static partial void LogFetchResultHandleResponse(ILogger logger, Redacted<string> handle, string status, int statusCode);

    [LoggerMessage(12, LogLevel.Warning, "FetchResultHandle for handle {Handle} returned unexpected HTTP {StatusCode}")]
    private static partial void LogFetchResultHandleUnexpectedHttp(ILogger logger, Redacted<string> handle, int statusCode);

    [LoggerMessage(13, LogLevel.Debug, "FetchResults sending GET to {Uri} for handle {Handle}")]
    private static partial void LogFetchResultsRequest(ILogger logger, Redacted<Uri> uri, Redacted<string> handle);

    [LoggerMessage(14, LogLevel.Debug, "FetchResults for handle {Handle} received HTTP {StatusCode}")]
    private static partial void LogFetchResultsResponse(ILogger logger, Redacted<string> handle, int statusCode);

    [LoggerMessage(15, LogLevel.Debug, "DiscardResults sending DELETE to {Uri} for handle {Handle}")]
    private static partial void LogDiscardResultsRequest(ILogger logger, Redacted<Uri> uri, Redacted<string> handle);

    [LoggerMessage(16, LogLevel.Debug, "DiscardResults for handle {Handle} completed with HTTP {StatusCode}")]
    private static partial void LogDiscardResultsResponse(ILogger logger, Redacted<string> handle, int statusCode);

    [LoggerMessage(17, LogLevel.Debug, "CancelQuery sending DELETE to {Uri} for requestId {RequestId}")]
    private static partial void LogCancelQueryRequest(ILogger logger, Redacted<Uri> uri, Redacted<string> requestId);

    [LoggerMessage(18, LogLevel.Debug, "CancelQuery for requestId {RequestId} completed with HTTP {StatusCode}")]
    private static partial void LogCancelQueryResponse(ILogger logger, Redacted<string> requestId, int statusCode);

    #endregion
}
