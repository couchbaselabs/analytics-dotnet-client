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
using Couchbase.Text.Json;

namespace Couchbase.Analytics2.Internal;

/// <summary>
/// A streaming response class for Analytics queries.
/// </summary>
/// <typeparam name="T"></typeparam>
/// <remarks>This is the default response type.</remarks>
internal class StreamingAnalyticsResult<T> : AnalyticsResultBase<T>
{
    private bool _hasReadToResult;
    private bool _hasReadResult;
    private bool _hasFinishedReading;
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
    }

    public override async IAsyncEnumerator<T> GetAsyncEnumerator(
        CancellationToken cancellationToken = new())
    {
        if (_hasReadResult)
        {
            yield break;
        }
        if (_hasFinishedReading)
        {
            _hasReadResult = true;
            yield break;
        }
        if (!_hasReadToResult)
        {
            throw new InvalidOperationException(
                $"{nameof(StreamingAnalyticsResult<T>)} has not been initialized, call InitializeAsync first");
        }

        if (_jsonReader == null)
        {
            throw new InvalidOperationException("_jsonReader is null");
        }

        await foreach (var result in _jsonReader.ReadObjectsAsync<T>(cancellationToken).ConfigureAwait(false))
        {
            yield return result;
        }

        _hasReadResult = true;

        await ReadResponseAttributes(cancellationToken).ConfigureAwait(false);
    }

    private async Task ReadResponseAttributes(CancellationToken cancellationToken)
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
                    var metrics = await _jsonReader.ReadObjectAsync<Metrics>(cancellationToken).ConfigureAwait(false);
                    MetaData.Metrics = new QueryMetrics(metrics);
                    break;
                case "results":
                    _hasReadToResult = true;
                    return;
                case "errors":
                case "warnings":
                case "status":
                    //Ignore for now
                    break;
            }
        }

        _hasFinishedReading = true;
    }

    public override void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _jsonReader?.Dispose();
        base.Dispose();
    }
}