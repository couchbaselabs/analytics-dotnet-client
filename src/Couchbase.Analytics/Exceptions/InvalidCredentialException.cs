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

using Couchbase.AnalyticsClient.Internal.Retry;

namespace Couchbase.AnalyticsClient.Exceptions;

/// <summary>
/// Thrown if the analytics service returns HTTP status code 401 or analytics error code 20000
/// </summary>
public class InvalidCredentialException : AnalyticsException
{
    public InvalidCredentialException()
    {
    }




    public InvalidCredentialException(string? message) : base(message)
    {
    }

    public InvalidCredentialException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    internal InvalidCredentialException(string? message, ErrorContext errorContext) : base(message, errorContext)
    {
    }
}
