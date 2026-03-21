using System.Collections.Concurrent;
using Couchbase.Analytics.Performer.Internal.Connections;
using Couchbase.Analytics.Performer.Internal.Logging;
using Couchbase.Analytics.Performer.Internal.Services;
using Couchbase.Grpc.Protocol.Columnar;
using Grpc.Core;
using Serilog;

namespace Couchbase.Analytics.Performer;

public class Program
{
    private static ConcurrentDictionary<string, ClusterConnection> _clusters = new();
    public static async Task Main(string[] args)
    {
        var loggerFactory = LoggingUtils.ConfigureLogging(out var minimumLevel);

        var (host, port) = ("localhost", 8060);

        var server = new Server
        {
            Ports =
            {
                new ServerPort(host, port, ServerCredentials.Insecure)
            },
            Services =
            {
                ColumnarService.BindService(new AnalyticsPerformerService(_clusters)),
                ColumnarCrossService.BindService(new AnalyticsPerformerCrossService(_clusters))
            }
        };

        server.Start();
        Log.Information(".NET Analytics Performer started on {Host}:{Port} at LogLevel {Level}", host, port, minimumLevel);

        Log.Information("Press any key to stop the server");
        Console.ReadKey();

        await server.ShutdownAsync().ConfigureAwait(false);
        LoggingUtils.ShutdownLogging();

        Log.Information("Press any key to exit");
        Console.ReadKey();
    }


}
