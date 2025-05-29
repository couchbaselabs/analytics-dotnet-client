namespace Couchbase.Analytics2;

public sealed class QueryMetrics
{
    internal QueryMetrics(
        string elapsedTime,
        string executionTime,
        string compileTime,
        string queueWaitTime,
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

    public string ElapsedTime { get; }

    public string ExecutionTime { get; }

    public string CompileTime { get; }

    public string QueueWaitTime { get; }

    public int ResultCount { get; }

    public int ResultSize { get; }

    public int ProcessedObjects { get; }

    public string BufferCacheHitRatio { get; }
}
