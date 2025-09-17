using System.Runtime.CompilerServices;

#if DEBUG
    [assembly: InternalsVisibleTo("Couchbase.Analytics.UnitTests")]
    [assembly: InternalsVisibleTo("Couchbase.AnalyticsClient.FunctionalTests")]
    [assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
#endif
[assembly: InternalsVisibleTo("Couchbase.Analytics.Performer")]