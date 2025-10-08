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

using System.Net;
using Couchbase.AnalyticsClient.Query;
using Couchbase.AnalyticsClient.Results;
using Couchbase.Core.Json;

namespace Couchbase.AnalyticsClient.Internal.Results;

internal abstract class AnalyticsResultBase : IQueryResult
{
    protected readonly Stream ResponseStream;
    protected readonly IDeserializer Serializer;
    private readonly IDisposable? _ownedForCleanup;
    private bool _disposed;

    /// <summary>
    /// Creates a new AnalyticsResultBase.
    /// </summary>
    /// <param name="responseStream"><see cref="Stream"/> to read.</param>
    /// <param name="serializer">The <see cref="ISerializer"/> to use for converting the response to an object.</param>
    /// <param name="ownedForCleanup">Additional object to dispose when complete.</param>
    protected AnalyticsResultBase(Stream responseStream, IDeserializer serializer, IDisposable? ownedForCleanup = null)
    {
        ResponseStream = responseStream ?? throw new ArgumentNullException(nameof(responseStream));
        Serializer = serializer;
        _ownedForCleanup = ownedForCleanup;
    }

    public abstract IAsyncEnumerator<AnalyticsRow> GetAsyncEnumerator(
        CancellationToken cancellationToken = default);

    public abstract Task InitializeAsync(CancellationToken cancellationToken = default);

    public IAsyncEnumerable<AnalyticsRow> Rows { get; protected set; }
    public QueryMetaData MetaData { get; protected set; }

    public IReadOnlyList<QueryError> Errors { get; protected set; }

    public HttpStatusCode? StatusCode { get; set; }

    public virtual void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        ResponseStream?.Dispose();
        _ownedForCleanup?.Dispose();
    }
}