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

using System.Text;
using Couchbase.AnalyticsClient.Exceptions;
using Couchbase.AnalyticsClient.HTTP;
using Couchbase.AnalyticsClient.Internal.HTTP;
using Couchbase.AnalyticsClient.Internal.Results;
using Couchbase.AnalyticsClient.Internal.Retry;
using Couchbase.AnalyticsClient.Options;
using Couchbase.AnalyticsClient.Results;
using Couchbase.Core.Json;
using Couchbase.Core.Utils;
using Microsoft.Extensions.Logging;

namespace Couchbase.AnalyticsClient.Internal;

internal sealed class AnalyticsService : HttpServiceBase, IAnalyticsService
{
    private readonly ClusterOptions _clusterOptions;
    private readonly ILogger<AnalyticsService> _logger;

    public AnalyticsService(ClusterOptions clusterOptions, ICouchbaseHttpClientFactory httpClientFactory,
        ILogger<AnalyticsService> logger) : base(httpClientFactory)
    {
        _clusterOptions = clusterOptions;
        _logger = logger;
        HttpClientFactory = httpClientFactory;
        Uri = clusterOptions.ConnectionStringValue!.GetAnalyticsServiceUri();
    }

    public Uri Uri { get; }

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
                _logger.LogDebug(
                    "Analytics query attempt {Attempt} starting for {ClientContextId} (elapsed: {Elapsed}ms)",
                    attempt + 1, options.ClientContextId, stopwatch.Elapsed.TotalMilliseconds);

                var result = await ExecuteQueryAsync(content, httpClient, options.AsStreaming, options.Deserializer, errorContext, cancellationToken).ConfigureAwait(false);

                // Always read errors from the result
                if (result.Errors is { Count: > 0 })
                {
                    // Per RFC: retry if ALL errors are retriable
                    if (AnalyticsErrorMapper.AreErrorsRetriable(result.Errors))
                    {
                        _logger.LogDebug("Received retriable server errors for ClientContextId {ClientContextId}, retrying...", options.ClientContextId);

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
                _logger.LogDebug(httpRequestException,
                    "Analytics query attempt {Attempt} for ClientContextId {ClientContextId} failed: {Error} (elapsed: {Elapsed}ms)",
                    attempt + 1, options.ClientContextId, httpRequestException.Message,
                    stopwatch.Elapsed.TotalMilliseconds);

                // "No successful connection(s)" is retryable
                if (httpRequestException.InnerException is AggregateException aggregateEx)
                {
                    lastException = new AnalyticsException("No connections could be established to any of the endpoints.", aggregateEx, errorContext);
                }

                if (!AnalyticsErrorMapper.IsRetriableHttpException(httpRequestException))
                {
                    _logger.LogDebug("HttpRequestException is not retriable, failing immediately");
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
}