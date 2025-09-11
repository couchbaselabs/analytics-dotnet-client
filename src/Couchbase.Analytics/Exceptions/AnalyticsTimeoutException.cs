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

using System.Runtime.Serialization;
using Couchbase.Analytics2.Internal.Retry;

namespace Couchbase.Analytics2.Exceptions;

/// <summary>
/// Thrown if the SDK detects a client-side timeout, or the server returns analytics error code 21002 (server-side timeout).
/// <remarks>This is the user's only indication that a request is potentially retriable, since the SDK automatically retries retriable operations until the timeout duration elapses.</remarks>
/// </summary>
public class AnalyticsTimeoutException : AnalyticsException
{
    public AnalyticsTimeoutException()
    {
    }

    protected AnalyticsTimeoutException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public AnalyticsTimeoutException(string? message) : base(message)
    {
    }

    public AnalyticsTimeoutException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
    internal AnalyticsTimeoutException(string? message, Exception? innerException, ErrorContext? errorContext) : base(message, innerException, errorContext)
    {
    }

    internal AnalyticsTimeoutException(string? message, ErrorContext? errorContext) : base(message, errorContext)
    {
    }
}