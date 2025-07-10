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

namespace Couchbase.Analytics2.Exceptions;

/// <summary>
/// Thrown if the analytics service returns a response with an error code other than 20000 or 21002.
/// </summary>
public class QueryException : ApplicationException
{
    public QueryException()
    {
    }

    protected QueryException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public QueryException(string? message) : base(message)
    {
    }

    public QueryException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    public int Code { get; init; }

    public string? ServerMessage { get; init; }
}