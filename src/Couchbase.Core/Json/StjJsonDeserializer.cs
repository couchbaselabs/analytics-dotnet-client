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

using System.Text.Json;

namespace Couchbase.Core;

/// <inheritdoc />>
public class StjJsonDeserializer(JsonSerializerOptions jsonSerializerOptions): IDeserializer
{
    /// <inheritdoc />
    public StjJsonDeserializer() : this(JsonSerializerOptions.Default)
    {
    }

    /// <inheritdoc />
    public ValueTask<T?> DeserializeAsync<T>(Stream stream,
        CancellationToken cancellationToken = default)
    {
        return JsonSerializer.DeserializeAsync<T>(stream, jsonSerializerOptions, cancellationToken);
    }

    /// <inheritdoc />
    public IJsonStreamReader CreateJsonStreamReader(Stream stream,
        CancellationToken cancellationToken = default)
    {
        return new JsonStreamReader(stream, jsonSerializerOptions);
    }
}