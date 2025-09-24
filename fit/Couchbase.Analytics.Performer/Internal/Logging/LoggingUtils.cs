using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace Couchbase.Analytics.Performer.Internal.Logging;

public static class LoggingUtils
{
    private const string LogLevelEnvVarName = "LOG_LEVEL";

    public static ILoggerFactory ConfigureLogging(out LogEventLevel minimumLevel)
    {
        var envLogLevel = Environment.GetEnvironmentVariable(LogLevelEnvVarName);
        minimumLevel = ParseLogLevelOrDefault(envLogLevel, LogEventLevel.Information);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(minimumLevel)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            // .WriteTo.File(
            //     path: "Logs/analytics-performer.log",
            //     rollingInterval: RollingInterval.Day,
            //     retainedFileCountLimit: 7,
            //     shared: true)
            .CreateLogger();

        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog();
        });

        return loggerFactory;
    }

    public static void ShutdownLogging()
    {
        Log.CloseAndFlush();
    }

    public static LogEventLevel ParseLogLevelOrDefault(string? value, LogEventLevel defaultLevel)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultLevel;
        }

        if (Enum.TryParse<LogEventLevel>(value, true, out var serilogLevel))
        {
            return serilogLevel;
        }

        if (Enum.TryParse<Microsoft.Extensions.Logging.LogLevel>(value, true, out var msLevel))
        {
            return msLevel switch
            {
                Microsoft.Extensions.Logging.LogLevel.Trace => LogEventLevel.Verbose,
                Microsoft.Extensions.Logging.LogLevel.Debug => LogEventLevel.Debug,
                Microsoft.Extensions.Logging.LogLevel.Information => LogEventLevel.Information,
                Microsoft.Extensions.Logging.LogLevel.Warning => LogEventLevel.Warning,
                Microsoft.Extensions.Logging.LogLevel.Error => LogEventLevel.Error,
                Microsoft.Extensions.Logging.LogLevel.Critical => LogEventLevel.Fatal,
                _ => defaultLevel
            };
        }

        return defaultLevel;
    }
}