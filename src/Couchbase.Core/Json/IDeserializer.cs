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

namespace Couchbase.Core.Json;

public interface IDeserializer
{
    /// <summary>
    /// Deserializes a stream of JSON into an object.
    /// </summary>
    /// <param name="stream">The stream of JSON bytes.</param>
    /// <param name="cancellationToken">An optional CancellationToken.</param>
    /// <typeparam name="T">The type to deserialize into.</typeparam>
    /// <returns>A ValueTask that can be awaited.</returns>
    ValueTask<T?> DeserializeAsync<T>(Stream stream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a Json stream reader based on STJ.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="cancellationToken">An optional CancellationToken.</param>
    /// <returns>The <see cref="IJsonStreamReader"/> to read the stream.</returns>
    IJsonStreamReader CreateJsonStreamReader(Stream stream, CancellationToken cancellationToken = default);
}
