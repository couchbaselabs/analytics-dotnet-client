using Couchbase.Core.Utils;
using Couchbase.Grpc.Protocol.Columnar;
using Google.Protobuf.WellKnownTypes;

namespace Couchbase.Analytics.Performer.Internal.Utils;

public static class EmptyResultOrFailureResponseExtensions
{
    public static EmptyResultOrFailureResponse GetResponseMetaData(this EmptyResultOrFailureResponse emptyResultOrFailureResponse,
        LightweightStopwatch stopwatch,
        Timestamp initiated,
        Exception? exception = null)
    {
        emptyResultOrFailureResponse.Metadata = new ResponseMetadata
        {
            ElapsedNanos = (long)stopwatch.Elapsed.TotalNanoseconds,
            Initiated = initiated,
        };

        if (exception is null)
        {
            emptyResultOrFailureResponse.EmptySuccess = true;
        }
        else
        {
            emptyResultOrFailureResponse.Error = exception.ToProtoError();
        }

        return emptyResultOrFailureResponse;
    }
}
