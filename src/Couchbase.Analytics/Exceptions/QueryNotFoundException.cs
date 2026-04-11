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

namespace Couchbase.AnalyticsClient.Exceptions;

/// <summary>
/// Exception thrown when the analytics service returns an HTTP Status 404 Not Found.
/// Currently only applies to the server async query requests API.
/// </summary>
public class QueryNotFoundException : AnalyticsException
{
    public QueryNotFoundException() : base("Query not found.") { }

    public QueryNotFoundException(string message) : base(message) { }

    public QueryNotFoundException(string message, Exception innerException) : base(message, innerException) { }

    internal QueryNotFoundException(string message, Exception innerException, Couchbase.AnalyticsClient.Internal.Retry.ErrorContext errorContext) : base(message, innerException, errorContext) { }
}
