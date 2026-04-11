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

using Couchbase.Core.Json;

namespace Couchbase.AnalyticsClient.Options;

/// <summary>
/// Options for fetching results of an asynchronous server-side query.
/// </summary>
public record FetchResultsOptions
{
    /// <summary>
    /// The deserializer to use when parsing the results.
    /// </summary>
    public IDeserializer? Deserializer { get; init; }

    /// <summary>
    /// Sets the deserializer to use when parsing the results.
    /// </summary>
    /// <param name="deserializer">The deserializer to use.</param>
    /// <returns>A new <see cref="FetchResultsOptions"/> with the deserializer set.</returns>
    public FetchResultsOptions WithDeserializer(IDeserializer deserializer) => this with { Deserializer = deserializer };
}
