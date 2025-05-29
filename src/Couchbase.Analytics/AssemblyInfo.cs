using System.Runtime.CompilerServices;

#if DEBUG
    [assembly: InternalsVisibleTo("Couchbase.Analytics.UnitTests")]
    [assembly: InternalsVisibleTo("Couchbase.Analytics2.FunctionalTests")]
    [assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
#endif

