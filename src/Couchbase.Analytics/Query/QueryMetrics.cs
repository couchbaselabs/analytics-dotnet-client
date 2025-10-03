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

using System.Text.Json.Serialization;
using Couchbase.Core.Json;

namespace Couchbase.AnalyticsClient.Query;

public sealed class QueryMetrics
{
    [JsonConverter(typeof(MillisecondsStringJsonConverter))]
    [JsonPropertyName("elapsedTime")]
    public TimeSpan? ElapsedTime { get; init; }

    [JsonConverter(typeof(MillisecondsStringJsonConverter))]
    [JsonPropertyName("executionTime")]
    public TimeSpan? ExecutionTime { get; init; }

    [JsonConverter(typeof(MillisecondsStringJsonConverter))]
    [JsonPropertyName("compileTime")]
    public TimeSpan? CompileTime { get; init; }

    [JsonConverter(typeof(MillisecondsStringJsonConverter))]
    [JsonPropertyName("queueWaitTime")]
    public TimeSpan? QueueWaitTime { get; init; }

    [JsonPropertyName("resultCount")]
    public int ResultCount { get; init; }

    [JsonPropertyName("resultSize")]
    public int ResultSize { get; init; }

    [JsonPropertyName("processedObjects")]
    public int ProcessedObjects { get; init; }

    [JsonPropertyName("bufferCacheHitRatio")]
    public string BufferCacheHitRatio { get; init; }
}