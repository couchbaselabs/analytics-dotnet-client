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

using System.Net;
using System.Net.Sockets;
using Couchbase.Analytics2.Exceptions;
using TimeoutException = System.TimeoutException;

namespace Couchbase.Analytics2.Internal.Retry;

/// <summary>
/// Static utility for mapping HTTP status codes to Analytics exceptions.
/// </summary>
internal static class AnalyticsErrorMapper
{
    /// <summary>
    /// Maps an HTTP status code to the appropriate Analytics exception type.
    /// </summary>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <returns>An AnalyticsException or one of its subclasses.</returns>
    public static AnalyticsException MapHttpErrorCode(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.Unauthorized => new InvalidCredentialException("Authentication failed - invalid credentials"),
            HttpStatusCode.ServiceUnavailable => new AnalyticsException("Service temporarily unavailable"),
            _ => new AnalyticsException($"HTTP {(int)statusCode} {statusCode}")
        };
    }

    /// <summary>
    /// Determines if an HttpRequestException is eligible for retry.
    /// </summary>
    /// <param name="exception">The HttpRequestException to check.</param>
    /// <returns>True if the exception indicates a retriable condition.</returns>
    public static bool IsRetriableHttpException(HttpRequestException exception)
    {
        var innerException = exception.InnerException;

        return innerException switch
        {
            SocketException => true,
            TimeoutException => true,
            TaskCanceledException => true,
            AggregateException => true, // That's the one we throw in EndpointConnectionManager if no connection could be made.
            _ => false
        };
    }

    /// <summary>
    /// Determines if an HTTP status code is eligible for retry.
    /// </summary>
    /// <param name="statusCode">The HTTP status code to check.</param>
    /// <returns>True if the status code indicates a retriable condition.</returns>
    public static bool IsRetriableStatusCode(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.ServiceUnavailable => true, // 503
            HttpStatusCode.BadGateway => true,
            HttpStatusCode.GatewayTimeout => true,
            HttpStatusCode.Unauthorized => false,      // Explicitly stating 401s are not retriable
            _ => false
        };
    }
}