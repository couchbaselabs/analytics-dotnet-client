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
using System.Net.Sockets;
using Couchbase.Analytics2.Exceptions;
using TimeoutException = System.TimeoutException;

namespace Couchbase.Analytics2.Internal.Retry;

/// <summary>
/// Static utility for mapping HTTP status codes and server error codes to Analytics exceptions.
/// </summary>
internal static class AnalyticsErrorMapper
{
    /// <summary>
    /// Maps an HTTP status code to the appropriate Analytics exception type.
    /// </summary>
    /// <param name="analyticsResult">The Http request result</param>
    /// /// <param name="errorContext">The request's error context</param>
    /// <returns>An AnalyticsException or one of its subclasses.</returns>
    internal static AnalyticsException MapHttpErrorCode(AnalyticsResultBase analyticsResult, ErrorContext errorContext)
    {
        errorContext.Errors.AddRange(analyticsResult.Errors);

        var statusCode = errorContext.StatusCode;
        return statusCode switch
        {
            HttpStatusCode.Unauthorized => new InvalidCredentialException($"Authentication failed - invalid credentials. Code: {(int)statusCode} - {statusCode}", errorContext),
            HttpStatusCode.ServiceUnavailable => new AnalyticsException($"Service temporarily unavailable. Code: {(int)statusCode} - {statusCode}", errorContext),
            HttpStatusCode.BadRequest => MapServerErrorCode(errorContext.Errors[0], errorContext),
            _ => new AnalyticsException($"HTTP {(int)statusCode} {statusCode}", errorContext)
        };
    }

    /// <summary>
    /// Maps a server error code to the appropriate Analytics exception type.
    /// </summary>
    /// <param name="error">The error from the server response.</param>
    /// <returns>An AnalyticsException or one of its subclasses.</returns>
    private static AnalyticsException MapServerErrorCode(Error error, ErrorContext errorContext)
    {
        return error.Code switch
        {
            20000 => new InvalidCredentialException(error.Message, errorContext),
            21002 => new AnalyticsTimeoutException($"{error.Message}. Error code: {error.Code}", errorContext),
            _ => new QueryException(error.Message, errorContext) { Code = error.Code, ServerMessage = error.Message }
        };
    }

    /// <summary>
    /// Processes an array of errors and returns the appropriate exception according to RFC rules.
    /// If the list contains at least one non-retriable error, the first non-retriable error is selected.
    /// Otherwise, the first retriable error is selected.
    /// </summary>
    /// <param name="errors">The errors array from the server response.</param>
    /// <param name="errorContext">The internal error context to be propagated</param>
    /// <returns>An AnalyticsException or one of its subclasses.</returns>
    internal static AnalyticsException MapServiceErrors(IReadOnlyList<Error> errors, ErrorContext errorContext)
    {
        if (errors == null || errors.Count == 0)
        {
            return new AnalyticsException("Unknown server error");
        }

        errorContext.Errors.AddRange(errors);

        // Find first non-retriable error
        var firstNonRetriable = errors.FirstOrDefault(e => !e.Retriable);
        if (firstNonRetriable != null)
        {
            return MapServerErrorCode(firstNonRetriable, errorContext);
        }

        // All errors are retriable, return the first one
        return MapServerErrorCode(errors[0], errorContext);
    }

    /// <summary>
    /// Determines if errors array indicates a retriable condition.
    /// Per RFC: eligible for retry if EVERY error in the array has retriable: true.
    /// </summary>
    /// <param name="errors">The errors array from the server response.</param>
    /// <returns>True if all errors are retriable.</returns>
    internal static bool AreErrorsRetriable(IReadOnlyList<Error> errors)
    {
        if (errors == null || errors.Count == 0)
        {
            return false;
        }

        return errors.All(e => e.Retriable);
    }

    /// <summary>
    /// Determines if an HttpRequestException is eligible for retry.
    /// </summary>
    /// <param name="exception">The HttpRequestException to check.</param>
    /// <returns>True if the exception indicates a retriable condition.</returns>
    internal static bool IsRetriableHttpException(HttpRequestException exception)
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
    internal static bool IsRetriableStatusCode(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.ServiceUnavailable => true, // 503
            HttpStatusCode.BadGateway => true,
            HttpStatusCode.GatewayTimeout => true,
            HttpStatusCode.Unauthorized => false, // Explicitly stating 401s are not retriable
            _ => false
        };
    }
}