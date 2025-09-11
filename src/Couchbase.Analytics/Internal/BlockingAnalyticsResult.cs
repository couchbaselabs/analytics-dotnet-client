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

using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Couchbase.Analytics2.Internal.Utils;
using Couchbase.Text.Json;

namespace Couchbase.Analytics2.Internal;

/// <summary>
/// A "blocking" result class for Analytics queries.
/// </summary>
/// <remarks>For large result sets use the <see cref="StreamingAnalyticsResult"/> class by setting <see cref="QueryOptions.AsStreaming"/> to true, which is the default.</remarks>
internal class BlockingAnalyticsResult : AnalyticsResultBase
{
    private IEnumerable<AnalyticsRow> _rows;
    private bool _enumerated;

    public BlockingAnalyticsResult(Stream responseStream, IDeserializer serializer, IDisposable? ownedForCleanup = null)
        : base(responseStream, serializer, ownedForCleanup)
    {
    }

    public override IAsyncEnumerator<AnalyticsRow> GetAsyncEnumerator(
        CancellationToken cancellationToken = default)
        => EnumerateRows(cancellationToken).GetAsyncEnumerator(cancellationToken);

    private async IAsyncEnumerable<AnalyticsRow> EnumerateRows(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_rows == null)
        {
            throw new InvalidOperationException(
                $"{nameof(BlockingAnalyticsResult)} has not been initialized, call InitializeAsync first");
        }

        if (_enumerated)
        {
            throw new InvalidOperationException("BlockingAnalyticsResult has already been enumerated");
        }

        _enumerated = true;

        foreach (var row in _rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return row;
        }
    }

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var reader = Serializer.CreateJsonStreamReader(ResponseStream, cancellationToken);

        if (!await reader.InitializeAsync(cancellationToken).ConfigureAwait(false))
        {
            ThrowHelper.ThrowArgumentNullException("No data received.");
        }

        MetaData = new QueryMetaData();

        var rows = new List<AnalyticsRow>();

        while (true)
        {
            var path = await reader.ReadToNextAttributeAsync(cancellationToken).ConfigureAwait(false);
            if (path == null)
            {
                break;
            }

            switch (path)
            {
                case "requestID" when reader.ValueType == typeof(string):
                    MetaData.RequestId = reader.Value?.ToString();
                    break;
                case "metrics":
                    var metrics = await reader.ReadObjectAsync<Metrics>(cancellationToken).ConfigureAwait(false);
                    MetaData.Metrics = new QueryMetrics(metrics);
                    break;
                case "results":
                {
                    await foreach (var token in reader.ReadTokensAsync(cancellationToken).ConfigureAwait(false))
                    {
                        rows.Add(new AnalyticsRow(token));
                    }

                    break;
                }
                case "errors":
                    var errors = await reader.ReadObjectAsync<Error[]>(cancellationToken).ConfigureAwait(false);
                    Errors = errors ?? Array.Empty<Error>();
                    break;
            }
        }

        _rows = rows;
        Rows = EnumerateRows(cancellationToken);
    }
}

#region POCOs
internal record Metrics
{
    [JsonConverter(typeof(MillisecondsStringJsonConverter))]
    public TimeSpan? elapsedTime { get; set; }

    [JsonConverter(typeof(MillisecondsStringJsonConverter))]
    public TimeSpan? executionTime { get; set; }

    [JsonConverter(typeof(MillisecondsStringJsonConverter))]
    public TimeSpan? compileTime { get; set; }

    [JsonConverter(typeof(MillisecondsStringJsonConverter))]
    public TimeSpan? queueWaitTime { get; set; }

    public int resultCount { get; set; }

    public int resultSize { get; set; }

    public int processedObjects { get; set; }

    public string bufferCacheHitRatio { get; set; }
}

internal record Plans
{
}

internal record AnalyticsResultData
{
    public string requestID { get; set; }
    public Signature signature { get; set; }
    public IEnumerable<AnalyticsRow> results { get; set; }
    public Plans plans { get; set; }
    public string status { get; set; }
    public Metrics metrics { get; set; }
    public IReadOnlyList<Error> errors { get; set; }
}

internal record Signature
{
    [JsonPropertyName("*")]
    public string signature { get; set; }
}
#endregion