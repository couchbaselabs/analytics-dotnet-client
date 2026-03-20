using Couchbase.AnalyticsClient.Query;
using Couchbase.Grpc.Protocol.Columnar;
using Google.Protobuf.WellKnownTypes;

namespace Couchbase.Analytics.Performer.Internal.Utils;

public static class ResponseMetaDataExtensions
{
    public static QueryResultMetadataResponse.Types.QueryMetadata ToResponseMetaData(this QueryMetaData metadata)
    {
        var sdkMetrics = metadata.Metrics!;
        var protoMetadata = new QueryResultMetadataResponse.Types.QueryMetadata()
        {
            Metrics = new QueryResultMetadataResponse.Types.QueryMetadata.Types.Metrics
            {
                ElapsedTime = new Duration
                {
                    Seconds = sdkMetrics.ElapsedTime!.Value.Seconds,
                    Nanos = sdkMetrics.ElapsedTime.Value.Nanoseconds
                },
                ExecutionTime = new Duration
                {
                    Seconds = sdkMetrics.ExecutionTime!.Value.Seconds,
                    Nanos = sdkMetrics.ExecutionTime.Value.Nanoseconds
                },
                ProcessedObjects = (ulong)sdkMetrics.ProcessedObjects,
                ResultCount = (ulong)sdkMetrics.ResultCount,
                ResultSize = (ulong)sdkMetrics.ResultSize
            }
        };

        if (metadata.RequestId is not null)
        {
            protoMetadata.RequestId = metadata.RequestId;
        }

        if (metadata.Warnings is not null)
        {
            foreach (var warnings in metadata.Warnings)
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
