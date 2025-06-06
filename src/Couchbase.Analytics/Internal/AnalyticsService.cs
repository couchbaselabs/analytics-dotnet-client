using System.Net;
using System.Text;
using Couchbase.Analytics2.Internal.HTTP;
using Couchbase.Analytics2.Internal.Serialization;
using Microsoft.Extensions.Logging;

namespace Couchbase.Analytics2.Internal;

internal class AnalyticsService : HttpServiceBase, IAnalyticsService
{
    private readonly ClusterOptions _options;
    private readonly ILogger<AnalyticsService> _logger;
    private const string ExecuteQueryPath = "/api/v1/request";
    private const string AnalyticsPriorityHeaderName = "Analytics-Priority";
    private readonly IJsonSerializer _jsonSerializer;


    public AnalyticsService(ClusterOptions options, ICouchbaseHttpClientFactory httpClientFactory, Uri endPoint,
        ILogger<AnalyticsService> logger, IJsonSerializer jsonSerializer) : base(httpClientFactory)
    {
        _options = options;
        _logger = logger;
        _jsonSerializer = jsonSerializer;
        HttpClientFactory = httpClientFactory;
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

            if (options.Priority)
            {
                request.Headers.Add(AnalyticsPriorityHeaderName, "true");
            }

            var httpClient = CreateHttpClient(options.Timeout);
            var response = await httpClient.SendAsync(request,
                    HttpClientFactory.DefaultCompletionOption,
                    options.CancellationToken)
                .ConfigureAwait(false);

            var stream = await response.Content.ReadAsStreamAsync()
                .ConfigureAwait(false);
            
            AnalyticsResultBase<T> result = null;
            if (options.AsStreaming)
            {
                result = new StreamingAnalyticsResult<T>(stream, httpClient);
            }
            else
            {
                result = new BlockingAnalyticsResult<T>(stream, _jsonSerializer, httpClient);
            }

            await result.InitializeAsync(options.CancellationToken)
                .ConfigureAwait(false);

            return result;
        }
    }
}