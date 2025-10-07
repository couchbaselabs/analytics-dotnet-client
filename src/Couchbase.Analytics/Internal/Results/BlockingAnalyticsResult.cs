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
using Couchbase.AnalyticsClient.Json;
using Couchbase.AnalyticsClient.Options;
using Couchbase.AnalyticsClient.Query;
using Couchbase.AnalyticsClient.Results;
using Couchbase.AnalyticsClient.Utils;
using Couchbase.Core.Json;

namespace Couchbase.AnalyticsClient.Internal.Results;

/// <summary>
/// A "blocking" result class for Analytics queries.
/// </summary>
/// <remarks>For large result sets use the <see cref="StreamingAnalyticsResult"/> class by setting <see cref="QueryOptions.AsStreaming"/> to true, which is the default.</remarks>
internal class BlockingAnalyticsResult : AnalyticsResultBase
{
    private IEnumerable<AnalyticsRow>? _rows;
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
                    MetaData.Metrics = await reader.ReadObjectAsync<QueryMetrics>(cancellationToken).ConfigureAwait(false);
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
                    var errors = await reader.ReadObjectAsync<QueryError[]>(cancellationToken).ConfigureAwait(false);
                    Errors = errors ?? Array.Empty<QueryError>();
                    break;
            }
        }

        _rows = rows;
        Rows = EnumerateRows(cancellationToken);
    }
}