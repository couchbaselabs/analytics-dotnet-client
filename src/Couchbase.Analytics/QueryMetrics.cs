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
 
using Couchbase.Analytics2.Internal;

namespace Couchbase.Analytics2;

public sealed class QueryMetrics
{
    internal QueryMetrics(
        TimeSpan? elapsedTime,
        TimeSpan? executionTime,
        TimeSpan? compileTime,
        TimeSpan? queueWaitTime,
        int resultCount,
        int resultSize,
        int processedObjects,
        string bufferCacheHitRatio
    )
    {
        ElapsedTime = elapsedTime;
        ExecutionTime = executionTime;
        CompileTime = compileTime;
        QueueWaitTime = queueWaitTime;
        ResultCount = resultCount;
        ResultSize = resultSize;
        ProcessedObjects = processedObjects;
        BufferCacheHitRatio = bufferCacheHitRatio;
    }

    internal QueryMetrics(Metrics metrics)
        : this(metrics.elapsedTime, metrics.executionTime, metrics.compileTime, metrics.queueWaitTime, metrics.resultCount, metrics.resultSize, metrics.processedObjects, metrics.bufferCacheHitRatio)
    {
    }

    public TimeSpan? ElapsedTime { get; }

    public TimeSpan? ExecutionTime { get; }

    public TimeSpan? CompileTime { get; }

    public TimeSpan? QueueWaitTime { get; }

    public int ResultCount { get; }

    public int ResultSize { get; }

    public int ProcessedObjects { get; }

    public string BufferCacheHitRatio { get; }
}