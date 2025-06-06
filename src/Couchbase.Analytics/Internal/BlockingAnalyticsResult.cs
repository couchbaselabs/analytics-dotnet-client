using System.Text.Json.Serialization;
using Couchbase.Analytics2.Internal.Serialization;
using Couchbase.Analytics2.Internal.Utils;

namespace Couchbase.Analytics2.Internal;

internal class BlockingAnalyticsResult<T> : AnalyticsResultBase<T>
{
    private readonly IJsonSerializer _jsonSerializer;
    private IEnumerable<T> _rows;
    private bool _enumerated;

    public BlockingAnalyticsResult(Stream responseStream, IJsonSerializer jsonSerializer, IDisposable? ownedForCleanup = null) 
        : base(responseStream, ownedForCleanup)
    {
        _jsonSerializer = jsonSerializer;
    }

    public override IAsyncEnumerator<T> GetAsyncEnumerator(
        CancellationToken cancellationToken = new())
    {
        if (_rows == null)
        {
            throw new InvalidOperationException(
                $"{nameof(BlockingAnalyticsResult<T>)} has not been initialized, call InitializeAsync first");
        }

        if (_enumerated)
        {
            throw new InvalidOperationException("BlockingAnalyticsResult<T> has already been enumerated");
        }

        _enumerated = true;
        return _rows.ToAsyncEnumerable().GetAsyncEnumerator(cancellationToken);
    }

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var body = await _jsonSerializer.DeserializeAsync<AnalyticsResultData<T>>(ResponseStream, cancellationToken)
            .ConfigureAwait(false);

        if (body == null)
        {
            ThrowHelper.ThrowArgumentNullException("No data received.");
        }

        MetaData = new QueryMetaData
        {
            Metrics = new QueryMetrics(body.metrics),
            RequestId = body.requestID,
           // Warnings = body.warnings
        };

        _rows = body.results;
    }
}

#region POCOs
internal record Metrics
{
    public string elapsedTime { get; set; }
    public string executionTime { get; set; }
    public string compileTime { get; set; }
    public string queueWaitTime { get; set; }
    public int resultCount { get; set; }
    public int resultSize { get; set; }
    public int processedObjects { get; set; }
    public string bufferCacheHitRatio { get; set; }
}

internal record Plans
{
}

internal record AnalyticsResultData<T>
{
    public string requestID { get; set; }
    public Signature signature { get; set; }
    public IEnumerable<T> results { get; set; }
    public Plans plans { get; set; }
    public string status { get; set; }
    public Metrics metrics { get; set; }
    public IReadOnlyList<Error> errors { get; protected set; }
}

internal record Signature
{
    [JsonPropertyName("*")]
    public string signature { get; set; }
}
#endregion
