using Couchbase.AnalyticsClient.Query;
using Couchbase.Grpc.Protocol.Columnar;
using Google.Protobuf.WellKnownTypes;

namespace Couchbase.Analytics.Performer.Internal.Utils;

public static class ResponseMetaDataExtensions
{
    public static QueryResultMetadataResponse.Types.QueryMetadata ToResponseMetaData(this QueryMetaData metadata)
    {
        var protoMetadata = new QueryResultMetadataResponse.Types.QueryMetadata();

        if (metadata.Metrics is { } sdkMetrics)
        {
            var protoMetrics = new QueryResultMetadataResponse.Types.QueryMetadata.Types.Metrics
            {
                ProcessedObjects = (ulong)sdkMetrics.ProcessedObjects,
                ResultCount = (ulong)sdkMetrics.ResultCount,
                ResultSize = (ulong)sdkMetrics.ResultSize
            };

            if (sdkMetrics.ElapsedTime is { } elapsed)
            {
                protoMetrics.ElapsedTime = new Duration
                {
                    Seconds = elapsed.Seconds,
                    Nanos = elapsed.Nanoseconds
                };
            }

            if (sdkMetrics.ExecutionTime is { } executed)
            {
                protoMetrics.ExecutionTime = new Duration
                {
                    Seconds = executed.Seconds,
                    Nanos = executed.Nanoseconds
                };
            }

            protoMetadata.Metrics = protoMetrics;
        }

        if (metadata.RequestId is { } requestId)
        {
            protoMetadata.RequestId = requestId;
        }

        if (metadata.Warnings is { } metaWarnings)
        {
            foreach (var warnings in metaWarnings)
            {
                protoMetadata.Warnings.Add(new QueryResultMetadataResponse.Types.QueryMetadata.Types.Warning()
                {
                    Code = (uint)warnings.Code,
                    Message = warnings.Message
                });
            }
        }
        return protoMetadata;
    }
}