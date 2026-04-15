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

using Couchbase.AnalyticsClient.Async;
using Couchbase.AnalyticsClient.Options;
using Couchbase.AnalyticsClient.Results;

namespace Couchbase.AnalyticsClient.Internal;

internal interface IAnalyticsService
{
    Uri Uri { get; }

    Task<IQueryResult> SendAsync(string statement, QueryOptions options, CancellationToken cancellationToken = default);

    // Async server request API methods

    /// <summary>
    /// Starts an asynchronous query on the server.
    /// Sends POST to /api/v1/request with mode=async.
    /// </summary>
    Task<QueryHandle> StartQueryAsync(string statement, StartQueryOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches the status of an async query from the server.
    /// Sends GET to /api/v1/request/status/{requestID}/{handleID}.
    /// </summary>
    Task<QueryResultHandle?> FetchResultHandleAsync(QueryHandle handle, FetchResultHandleOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches the results of a completed async query from the server.
    /// Sends GET to /api/v1/request/result/{requestID}/{handleID}.
    /// </summary>
    Task<IQueryResult> FetchResultsAsync(string requestId, string handlePath, FetchResultsOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Discards the results of an async query on the server.
    /// Sends DELETE to /api/v1/request/result/{requestID}/{handleID}.
    /// </summary>
    Task DiscardResultsAsync(string requestId, string handlePath, DiscardResultsOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels an active async query on the server.
    /// Sends DELETE to /api/v1/active_requests with the request_id.
    /// </summary>
    Task CancelQueryAsync(string requestId, CancelOptions options, CancellationToken cancellationToken = default);
}
