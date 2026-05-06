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

        bool disableConsoleRead = false;

        foreach (var arg in args)
        {
            var parameter = arg.Split('=');
            if (parameter.Length == 2)
            {
                switch (parameter[0])
                {
                    case "disableConsoleRead":
                        disableConsoleRead = bool.Parse(parameter[1]);
                        break;
                }
            }
        }

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

        if (disableConsoleRead)
        {
            Log.Information("Running in headless mode, waiting for shutdown signal");
            var shutdownEvent = new ManualResetEventSlim(false);
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                shutdownEvent.Set();
            };
            AppDomain.CurrentDomain.ProcessExit += (_, _) => shutdownEvent.Set();
            shutdownEvent.Wait();
        }
        else
        {
            Log.Information("Press any key to stop the server");
            Console.ReadKey();
        }

        await server.ShutdownAsync().ConfigureAwait(false);
        LoggingUtils.ShutdownLogging();

        if (!disableConsoleRead)
        {
            Log.Information("Press any key to exit");
            Console.ReadKey();
        }
    }
}
