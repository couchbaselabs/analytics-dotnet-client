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
using Couchbase.Analytics2.Exceptions;
using Couchbase.Analytics2.Internal.HTTP;
using Couchbase.Analytics2.Internal.Retry;
using Couchbase.Text.Json;
using Couchbase.Text.Json.Utils;
using Microsoft.Extensions.Logging;
using TimeoutException = Couchbase.Analytics2.Exceptions.TimeoutException;

namespace Couchbase.Analytics2.Internal;

internal class AnalyticsService : HttpServiceBase, IAnalyticsService
{
    private readonly ClusterOptions _clusterOptions;
    private readonly ILogger<AnalyticsService> _logger;
    private const string ExecuteQueryPath = "api/v1/request";
    private readonly IDeserializer _serializer;

    public AnalyticsService(ClusterOptions clusterOptions, ICouchbaseHttpClientFactory httpClientFactory,
        ILogger<AnalyticsService> logger, IDeserializer serializer) : base(httpClientFactory)
    {
        _clusterOptions = clusterOptions;
        _logger = logger;
        _serializer = serializer;
        HttpClientFactory = httpClientFactory;
        var endPoint  = clusterOptions.ConnectionStringValue!.GetDnsBootStrapUri();
        Uri = new Uri($"{endPoint}{ExecuteQueryPath}");
    }

    public Uri Uri { get; private set; }

    public async Task<IQueryResult> SendAsync(string statement, QueryOptions options, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithRetryAsync(statement, options, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Core query execution logic - the golden path for sending analytics requests.
    /// </summary>
    private async Task<IQueryResult> ExecuteQueryAsync(StringContent content, HttpClient httpClient, bool asStreaming, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, Uri)
        {
            Content = content
        };

        try
        {
            var response = await httpClient.SendAsync(request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken)
                .ConfigureAwait(false);

            var stream = await response.Content.ReadAsStreamAsync()
                .ConfigureAwait(false);

            AnalyticsResultBase result = asStreaming
                ? new StreamingAnalyticsResult(stream, _serializer, httpClient)
                : new BlockingAnalyticsResult(stream, _serializer, httpClient);

            result.StatusCode = response.StatusCode;

            await result.InitializeAsync(cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                throw AnalyticsErrorMapper.MapHttpErrorCode(response.StatusCode);
            }

            return result;
        }
        catch (TaskCanceledException taskCanceledEx)
        {
            throw new TimeoutException("The analytics request was canceled via its cancellation token.", taskCanceledEx);
        }
    }

    /// <summary>
    /// Retry wrapper around the core query execution logic.
    /// </summary>
    private async Task<IQueryResult> ExecuteWithRetryAsync(string statement, QueryOptions options, CancellationToken cancellationToken = default)
    {
        var stopwatch = LightweightStopwatch.StartNew();
        Exception lastException = new AnalyticsException("Maximum retries exceeded without success.");

        var timeout = options.Timeout ?? _clusterOptions.TimeoutOptions.QueryTimeout;

        var body = options.GetFormValuesAsJson(statement);
        using var content = new StringContent(body, Encoding.UTF8, MediaType.Json);

        // This timeout is per-attempt i.e. per http.SendAsync().
        var httpClient = CreateHttpClient(timeout);

        var maxRetries = options.MaxRetries ?? _clusterOptions.MaxRetries;

        for (uint attempt = 0; attempt <= maxRetries; attempt++)
        {
            // We observe the overall timeout across all retries.
            if (stopwatch.Elapsed > timeout)
            {
                ThrowGlobalTimeout(lastException, stopwatch.Elapsed);
            }
            try
            {
                _logger.LogDebug(
                    "Analytics query attempt {Attempt} starting for {ClientContextId} (elapsed: {Elapsed}ms)",
                    attempt + 1, options.ClientContextId, stopwatch.Elapsed.TotalMilliseconds);

                var result = await ExecuteQueryAsync(content, httpClient, options.AsStreaming, cancellationToken).ConfigureAwait(false);

                // Always read errors from the result
                if (result.Errors is { Count: > 0 })
                {
                    // Per RFC: retry if ALL errors are retriable
                    if (AnalyticsErrorMapper.AreErrorsRetriable(result.Errors))
                    {
                        _logger.LogDebug("Received retriable server errors for ClientContextId {ClientContextId}, retrying...", options.ClientContextId);
                        lastException = AnalyticsErrorMapper.MapServiceErrors(result.Errors);
                        await RetryUtils.BackoffAsync(attempt, cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    // Non-retriable server errors - throw
                    throw AnalyticsErrorMapper.MapServiceErrors(result.Errors);
                }

                return result;
            }
            catch (HttpRequestException httpRequestException)
            {
                _logger.LogDebug(httpRequestException,
                    "Analytics query attempt {Attempt} for ClientContextId {ClientContextId} failed: {Error} (elapsed: {Elapsed}ms)",
                    attempt + 1, options.ClientContextId, httpRequestException.Message,
                    stopwatch.Elapsed.TotalMilliseconds);

                // "No successful connection" is retryable
                if (httpRequestException.InnerException is AggregateException aggregateEx)
                {
                    lastException = new AnalyticsException("No connections could be established to any of the endpoints.", aggregateEx);
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
                // Check if we exceeded the query timeout
                if (stopwatch.Elapsed > timeout)
                {
                    ThrowGlobalTimeout(lastException, stopwatch.Elapsed, operationCanceledException);
                }

                lastException = operationCanceledException;

                await RetryUtils.BackoffAsync(attempt, cancellationToken).ConfigureAwait(false);
            }
        }
        throw lastException;
    }

    private static void ThrowGlobalTimeout(Exception lastException, TimeSpan elapsed, Exception? inner = null)
    {
        var timeoutException = new TimeoutException(
            $"Analytics query timed-out after {elapsed.TotalSeconds:F2} seconds.", inner);
        timeoutException.LastError = lastException;
        throw timeoutException;
    }
}