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
using Couchbase.AnalyticsClient.Query;
using Couchbase.AnalyticsClient.Results;
using Couchbase.Core.Json;

namespace Couchbase.AnalyticsClient.Internal.Results;

/// <summary>
/// A streaming response class for Analytics queries.
/// </summary>
/// <typeparam name="T"></typeparam>
/// <remarks>This is the default response type.</remarks>
internal class StreamingAnalyticsResult : AnalyticsResultBase
{
    private bool _hasReadToResult;
    private int _enumerated; // 0 = not started, 1 = started (atomic via Interlocked)
    private IJsonStreamReader _jsonReader;
    private bool _disposed;

    public StreamingAnalyticsResult(Stream stream, IDeserializer serializer, IDisposable? ownedForCleanup = null)
        : base(stream, serializer, ownedForCleanup)
    {
        _jsonReader = serializer.CreateJsonStreamReader(stream);
    }

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (!await _jsonReader.InitializeAsync(cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        await ReadResponseAttributes(cancellationToken).ConfigureAwait(false);

        Rows = EnumerateRows(cancellationToken);
    }

    public override IAsyncEnumerator<AnalyticsRow> GetAsyncEnumerator(
        CancellationToken cancellationToken = default)
        => EnumerateRows(cancellationToken).GetAsyncEnumerator(cancellationToken);

    private async IAsyncEnumerable<AnalyticsRow> EnumerateRows(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (Interlocked.CompareExchange(ref _enumerated, 1, 0) != 0)
        {
            throw new InvalidOperationException(
                "Query results can only be enumerated once. The result stream has already been consumed.");
        }

        if (!_hasReadToResult)
        {
            throw new InvalidOperationException(
                $"{nameof(StreamingAnalyticsResult)} has not been initialized, call InitializeAsync first");
        }

        if (_jsonReader == null)
        {
            throw new InvalidOperationException("_jsonReader is null");
        }

        await foreach (var token in _jsonReader.ReadTokensAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return new AnalyticsRow(token);
        }

        await ReadResponseAttributes(cancellationToken).ConfigureAwait(false);
    }

    private async Task ReadResponseAttributes(CancellationToken cancellationToken = default)
    {
        if (_jsonReader == null)
        {
            throw new InvalidOperationException("_jsonReader is null");
        }

        MetaData = new QueryMetaData();

        _hasReadToResult = false;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var path = await _jsonReader.ReadToNextAttributeAsync(cancellationToken).ConfigureAwait(false);
            if (path == null)
            {
                break;
            }

            switch (path)
            {
                case "requestID" when _jsonReader.ValueType == typeof(string):
                    MetaData.RequestId = _jsonReader.Value?.ToString();
                    break;
                case "metrics":
                    MetaData.Metrics = await _jsonReader.ReadObjectAsync<QueryMetrics>(cancellationToken).ConfigureAwait(false);
                    break;
                case "results":
                    _hasReadToResult = true;
                    return;
                case "errors":
                    var errors = await _jsonReader.ReadObjectAsync<QueryError[]>(cancellationToken).ConfigureAwait(false);
                    Errors = errors ?? Array.Empty<QueryError>();
                    break;
                case "warnings":
                case "status":
                    //Ignore for now
                    break;
            }
        }

    }

    public override void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _jsonReader?.Dispose();
        base.Dispose();
    }

    public override async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        _jsonReader?.Dispose();
        await base.DisposeAsync().ConfigureAwait(false);
    }
}
