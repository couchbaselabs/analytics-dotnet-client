using Couchbase.Grpc.Protocol.Columnar;

namespace Couchbase.Analytics.Performer.Internal.Utils;

public static class ExecuteQueryRequestExtensions
{
    public static bool HasClusterLevel(this ExecuteQueryRequest request)
    {
        return request.LevelCase ==
               ExecuteQueryRequest.LevelOneofCase.ClusterLevel;
    }
}
