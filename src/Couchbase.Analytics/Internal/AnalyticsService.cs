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
using System.Text;
using Couchbase.Analytics2.Exceptions;
using Couchbase.Analytics2.Internal.HTTP;
using Couchbase.Text.Json;
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
        var body = options.GetFormValuesAsJson(statement);

        using (var content =
               new StringContent(body, Encoding.UTF8, MediaType.Json))
        {
            var request = new HttpRequestMessage(HttpMethod.Post, Uri)
            {
                Content = content
            };

            var httpClient = CreateHttpClient(options.Timeout);

            HttpResponseMessage? response = null;
            try
            {
                response = await httpClient.SendAsync(request,
                        HttpCompletionOption.ResponseHeadersRead,
                        options.CancellationToken)
                    .ConfigureAwait(false);
            }
            catch (HttpRequestException e)
            {
                if (e.InnerException != null && e.InnerException is AnalyticsException analyticsException)
                {
                    throw analyticsException;
                }
            }

            var stream = await response.Content.ReadAsStreamAsync()
                .ConfigureAwait(false);

            AnalyticsResultBase<T> result = null;
            if (options.AsStreaming)
            {

                result = new StreamingAnalyticsResult<T>(stream, _serializer, httpClient);
            }
            else
            {
               result = new BlockingAnalyticsResult<T>(stream, _serializer, httpClient);
            }

            await result.InitializeAsync(options.CancellationToken)
                .ConfigureAwait(false);

            return result;
        }
    }
}