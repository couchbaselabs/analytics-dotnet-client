using System.Runtime.CompilerServices;

#if !SIGNING
[assembly: InternalsVisibleTo("Couchbase.Analytics.UnitTests")]
[assembly: InternalsVisibleTo("Couchbase.AnalyticsClient.FunctionalTests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
[assembly: InternalsVisibleTo("Couchbase.Analytics.Performer")]
#endif
