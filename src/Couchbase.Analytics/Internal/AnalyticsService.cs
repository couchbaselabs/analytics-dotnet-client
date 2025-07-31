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

namespace Couchbase.Analytics2.Internal;

internal class AnalyticsService : HttpServiceBase, IAnalyticsService
{
    private readonly ClusterOptions _options;
    private readonly ILogger<AnalyticsService> _logger;
    private const string ExecuteQueryPath = "/api/v1/request";
    private readonly IDeserializer _serializer;

    public AnalyticsService(ClusterOptions options, ICouchbaseHttpClientFactory httpClientFactory,
        ILogger<AnalyticsService> logger, IDeserializer serializer) : base(httpClientFactory)
    {
        _options = options;
        _logger = logger;
        _serializer = serializer;
        HttpClientFactory = httpClientFactory;
        var endPoint  = options.ConnectionStringValue!.GetDnsBootStrapUri();
        Uri = new Uri($"https://{endPoint.Host}:{endPoint.Port}{ExecuteQueryPath}");
    }

    public Uri Uri { get; private set; }

    public async Task<IQueryResult<T>> SendAsync<T>(string statement, QueryOptions options)
    {
        return await ExecuteWithRetryAsync<T>(statement, options).ConfigureAwait(false);
    }

    /// <summary>
    /// Core query execution logic - the golden path for sending analytics requests.
    /// </summary>
    private async Task<IQueryResult<T>> ExecuteQueryAsync<T>(string statement, QueryOptions options)
    {
        var body = options.GetFormValuesAsJson(statement);

        using var content = new StringContent(body, Encoding.UTF8, MediaType.Json);
        var request = new HttpRequestMessage(HttpMethod.Post, Uri)
        {
            Content = content
        };

        var httpClient = CreateHttpClient(options.Timeout);

        var response = await httpClient.SendAsync(request,
                HttpCompletionOption.ResponseHeadersRead,
                options.CancellationToken)
            .ConfigureAwait(false);

        var stream = await response.Content.ReadAsStreamAsync()
            .ConfigureAwait(false);

        AnalyticsResultBase<T> result = options.AsStreaming
            ? new StreamingAnalyticsResult<T>(stream, _serializer, httpClient)
            : new BlockingAnalyticsResult<T>(stream, _serializer, httpClient);

        result.StatusCode = response.StatusCode;

        await result.InitializeAsync(options.CancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw AnalyticsErrorMapper.MapHttpErrorCode(response.StatusCode);
        }

        return result;
    }

    /// <summary>
    /// Retry wrapper around the core query execution logic.
    /// </summary>
    private async Task<IQueryResult<T>> ExecuteWithRetryAsync<T>(string statement, QueryOptions options)
    {
        var stopwatch = LightweightStopwatch.StartNew();
        Exception lastException = new AnalyticsException("Maximum retries exceeded without success.");

        for (uint attempt = 0; attempt <= _options.MaxRetries; attempt++)
        {
            try
            {
                _logger.LogDebug(
                    "Analytics query attempt {Attempt} starting for {ClientContextId} (elapsed: {Elapsed}ms)",
                    attempt + 1, options.ClientContextId, stopwatch.Elapsed.TotalMilliseconds);

                var result = await ExecuteQueryAsync<T>(statement, options).ConfigureAwait(false);

                // Always read errors from the result
                if (result.Errors is { Count: > 0 })
                {
                    // Per RFC: retry if ALL errors are retriable
                    if (AnalyticsErrorMapper.AreErrorsRetriable(result.Errors))
                    {
                        _logger.LogDebug("Received retriable server errors for ClientContextId {ClientContextId}, retrying...", options.ClientContextId);
                        lastException = AnalyticsErrorMapper.MapServiceErrors(result.Errors);
                        await RetryUtils.BackoffAsync(attempt, options.CancellationToken).ConfigureAwait(false);
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

                await RetryUtils.BackoffAsync(attempt, options.CancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException operationCanceledException)
            {
                // Check if we exceeded the query timeout
                if (stopwatch.Elapsed >= _options.TimeoutOptions.QueryTimeout)
                {
                    var timeoutException = new Exceptions.TimeoutException($"Analytics query timed-out after {stopwatch.Elapsed.TotalSeconds:F2} seconds",
                        operationCanceledException);

                    timeoutException.LastError = lastException;

                    throw timeoutException;
                }

                lastException = operationCanceledException;

                await RetryUtils.BackoffAsync(attempt, options.CancellationToken).ConfigureAwait(false);
            }
        }
        throw lastException;
    }
}