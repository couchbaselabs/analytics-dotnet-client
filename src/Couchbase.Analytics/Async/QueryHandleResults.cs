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

using Couchbase.AnalyticsClient.Internal;
using Couchbase.AnalyticsClient.Results;
using Couchbase.Core.Json;

namespace Couchbase.AnalyticsClient.Async;

/// <summary>
/// Provides access to the results of a completed asynchronous query.
/// Obtained from <see cref="QueryStatus.GetResults"/> when the query status is "success".
/// </summary>
public class QueryHandleResults
{
    private readonly string _handle;
    private readonly IAnalyticsService _analyticsService;
    private readonly IDeserializer _deserializer;
    private readonly TimeSpan? _requestTimeout;

    internal QueryHandleResults(string handle, IAnalyticsService analyticsService, IDeserializer deserializer, TimeSpan? requestTimeout = null)
    {
        _handle = handle ?? throw new ArgumentNullException(nameof(handle));
        _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
        _deserializer = deserializer ?? throw new ArgumentNullException(nameof(deserializer));
        _requestTimeout = requestTimeout;
    }

    /// <summary>
    /// Streams all query result rows from the server.
    /// The returned <see cref="IQueryResult"/> behaves the same as the synchronous query API result.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>An <see cref="IQueryResult"/> that can be used to enumerate the result rows.</returns>
    public async Task<IQueryResult> StreamAllAsync(CancellationToken cancellationToken = default)
    {
        return await _analyticsService.FetchResultsAsync(_handle, _requestTimeout, _deserializer, cancellationToken)
            .ConfigureAwait(false);
    }
}
