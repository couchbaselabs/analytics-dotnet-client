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
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using Couchbase.Analytics2.Internal.Retry;

namespace Couchbase.Analytics2.Exceptions;

/// <summary>
/// Base exception type for Analytics.
/// </summary>
public class AnalyticsException : Exception
{
    internal ErrorContext? ErrorContext { get; set; }

    public AggregateException? AggregateException { get; }

    public AnalyticsException()
    {
    }

    protected AnalyticsException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public AnalyticsException(string? message) : base(message)
    {
    }

    public AnalyticsException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    internal AnalyticsException(string? message, ErrorContext? errorContext) : base(message)
    {
        ErrorContext = errorContext;
    }

    internal AnalyticsException(string? message, Exception? innerException, ErrorContext? errorContext) : base(message, innerException)
    {
        ErrorContext = errorContext;
    }

    internal AnalyticsException(string? message, AggregateException? aggregateException, ErrorContext? errorContext) : base(message)
    {
        AggregateException = aggregateException;
        ErrorContext = errorContext;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append(Message);
        sb.Append(InnerException?.Message);
        sb.Append($" Context Info: {ErrorContext?.ToString()}");
        return sb.ToString();
    }
}